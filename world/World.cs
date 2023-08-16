namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.EntityTypes;
using Jaket.UI;

/// <summary> Class that manages objects in the level, such as doors and etc. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class World : MonoSingleton<World>
{
    /// <summary> List of all doors in the level, updated when a level is loaded. </summary>
    private List<GameObject> doors = new();
    /// <summary> List of open doors, cleared only when the player enters a new level. </summary>
    private List<int> opened = new();

    /// <summary> List of all object activators needed for synchronization at all levels. </summary>
    private List<Activator> activators = new();
    /// <summary> List of activated objects, cleared only when the player enters a new level. </summary>
    private List<int> activated = new();

    /// <summary> Name of the last loaded scene. </summary>
    private string LastScene;
    /// <summary> Level 5-4 contains a unique boss that needs to be dealt with separately. </summary>
    public Leviathan Leviathan;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("World", Plugin.Instance.transform).AddComponent<World>();

        // updates the list of objects in the level when the scene is loaded
        SceneManager.sceneLoaded += (scene, mode) => Instance.Recache();

        // create activators to synchronize different things in the level
        World.Instance.activators.AddRange(new[]
        {
            // there is a door in the arena through which V2 escapes and you also need to synchronize the outro and the exit building
            Activators.FindByNameAndActiveParentOfParent("Level 4-4", "Checkpoint Activator", obj =>
            {
                obj?.transform.parent.gameObject.SetActive(true);
                obj?.transform.parent.parent.Find("Wall").gameObject.SetActive(false);
            }),
            Activators.FindByNameAndActiveParent("Level 4-4", "BossOutro"),
            Activators.FindByNameAndActiveParent("Level 4-4", "ExitBuilding Raise", obj =>
            {
                var bulding = obj.transform.parent.Find("ExitBuilding");
                bulding.GetComponent<Door>().Close();
                bulding.GetChild(14).gameObject.SetActive(true);
            }),

            // there is a checkpoint deactivator at level 5-1, the deactivation of which needs to be synchronized
            Activators.FindByNameAndActiveParentOfParent("Level 5-1", "CheckPointsUndisabler"),

            // Minos & Sisyphus has a unique cutscene and a non-working exit from the level
            Activators.FindByNameAndActiveParent("Level P-1", "MinosPrimeIntro", disposable: true),
            Activators.FindByNameAndActiveParent("Level P-1", "End", obj => obj?.transform.parent.GetChild(7).gameObject.SetActive(false)),
            Activators.FindByNameAndActiveParent("Level P-2", "PrimeIntro", disposable: true),
            Activators.FindByNameAndActiveParent("Level P-2", "Outro", obj => obj?.transform.parent.GetChild(7).gameObject.SetActive(false))
        });
    }

    /// <summary> Updates the list of objects in the level. </summary>
    public void Recache()
    {
        // find all the doors on the level, because the old ones have already been destroyed
        doors.Clear();

        // FindGameObjectsWithTag may be faster, but doesn't work with inactive objects
        foreach (var door in Resources.FindObjectsOfTypeAll<Door>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<FinalDoor>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<DoorOpener>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<BigDoorOpener>()) doors.Add(door.gameObject);

        // sort doors by position to make sure their order is the same for different clients
        doors.Sort((d1, d2) => d1.transform.position.sqrMagnitude.CompareTo(d2.transform.position.sqrMagnitude));

        // initialize the activators created for the current level
        activators.ForEach(activator =>
        {
            if (activator.Level == SceneHelper.CurrentScene) activator.Init();
        });

        // clear the list of open doors and activated objects if the player has entered a new level
        if (SceneHelper.CurrentScene != LastScene)
        {
            LastScene = SceneHelper.CurrentScene;

            opened.Clear();
            activated.Clear();
        }
        // but if the player restarted the same level, then you need to open all the doors and reactivate the objects
        else
        {
            opened.ForEach(index => OpenDoor(index, false));
            activated.ForEach(index => ActivateObject(index, false));
        }
    }

    #region doors & activators

    /// <summary> Opens the door by the given index. </summary>
    public void OpenDoor(int index, bool save = true)
    {
        // save the index of the open door so that even after loading to the checkpoint the door remains open
        if (save) opened.Add(index);

        // find the door by index and open it to prevent getting stuck in a room
        var door = doors[index];
        if (door != null) OpenDoor(door);
    }

    /// <summary> Opens given door. </summary>
    public void OpenDoor(GameObject obj)
    {
        // the most common doors, there are plenty of them on the level, but they may be blocked
        if (obj.TryGetComponent<Door>(out var door)) door.Unlock();

        // the final doors can open themselves or only after defeating the boss
        // in the latter case they may not open on the client, so you need to do it yourself
        else if (obj.TryGetComponent<FinalDoor>(out var final)) final.Open();

        // the game has a bunch of different triggers that work only on the host
        else if (obj.TryGetComponent<DoorOpener>(out var opener))
        {
            opener.gameObject.SetActive(true);
            opener.transform.parent.gameObject.SetActive(true);
        }

        // level 0-5 has a unique door that for some reason does not want to open itself
        else if (obj.TryGetComponent<BigDoorOpener>(out var big))
        {
            big.gameObject.SetActive(true); // this door is so~ unique
            big.transform.parent.GetChild(3).gameObject.SetActive(true);
        }
    }

    /// <summary> Activates the object by the given index. </summary>
    public void ActivateObject(int index, bool save = true)
    {
        // save the index of the activated object so that even after loading to the checkpoint the object remains active
        if (save) activated.Add(index);

        // some objects, such as cutscenes before bosses, don't need to be reactivated after loading to the checkpoint
        if (save || !activators[index].Disposable) activators[index].Activate();
    }

    #endregion
    #region serialization

    /// <summary> Informs all the client that the door is open. </summary>
    public void SendDoorOpening(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) return;

        // write door index to send to clients
        byte[] data = Writer.Write(w => w.Int(index));

        // notify each client that the door has opened so they don't get stuck in a room
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.OpenDoor));
    }

    /// <summary> Checks if the door should be open and opens if it should. </summary>
    public void CheckIfDoorOpen(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) return;

        // if the door is marked as open, then it must be reopened
        if (opened.Contains(index)) OpenDoor(obj);
    }

    /// <summary> Informs all the client that the object is active. </summary>
    public void SendObjectActivation(Activator activator)
    {
        int index = activators.IndexOf(activator);
        if (index == -1) return;

        // write activator index to send to clients
        byte[] data = Writer.Write(w => w.Int(index));

        // notify each client that the object has activated
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.ActivateObject));
    }

    #endregion
}