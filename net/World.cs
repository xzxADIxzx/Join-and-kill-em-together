namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary> Class that manages objects in the level, such as doors and etc. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class World : MonoSingleton<World>
{
    /// <summary> List of all doors in the level, updated when a level is loaded. </summary>
    private List<GameObject> doors = new();
    /// <summary> List of open doors, cleared only when the player enters a new level. </summary>
    private List<int> opened = new();

    /// <summary> Name of the last loaded scene. </summary>
    private string LastScene;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("World", Plugin.Instance.transform).AddComponent<World>();

        // updates the list of objects in the level when the scene is loaded
        SceneManager.sceneLoaded += (scene, mode) => Instance.Recache();
    }

    /// <summary> Updates the list of objects in the level. </summary>
    public void Recache()
    {
        Debug.LogWarning("RECACHE");
        // find all the doors on the level, because the old ones have already been destroyed
        doors.Clear();

        // FindGameObjectsWithTag may be faster, but doesn't work with inactive objects
        foreach (var door in Resources.FindObjectsOfTypeAll<Door>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<FinalDoor>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<BigDoorOpener>()) doors.Add(door.gameObject);

        // sort doors by position to make sure their order is the same for different clients
        doors.Sort((d1, d2) => d1.transform.position.sqrMagnitude.CompareTo(d2.transform.position.sqrMagnitude));

        // clear the list of open doors if the player has entered a new level
        if (SceneHelper.CurrentScene != LastScene)
        {
            opened.Clear();
            LastScene = SceneHelper.CurrentScene;
        }
        // but if the player just restarted the same level, then you need to open all the doors
        else opened.ForEach(index => OpenDoor(index, false));
    }

    #region door

    /// <summary> Opens the door by the given index. </summary>
    public void OpenDoor(int index, bool save = true)
    {
        // save the index of the open door so that even after reloading the level the door remains open
        if (save) opened.Add(index);

        // find the door by index and open it to prevent getting stuck in a room
        var door = doors[index];
        if (door != null) OpenDoor(door);
    }

    /// <summary> Opens given door. </summary>
    public void OpenDoor(GameObject obj)
    {
        // the most common doors, there are plenty of them on the level, but they may be blocked
        if (obj.TryGetComponent<Door>(out var door))
        {
            door.Unlock();
            return;
        }

        // the final doors can open themselves or only after defeating the boss
        // in the latter case they may not open on the client, so you need to do it yourself
        if (obj.TryGetComponent<FinalDoor>(out var final))
        {
            final.Open();
            return;
        }

        // level 0-5 has a unique door that for some reason does not want to open itself
        if (obj.TryGetComponent<BigDoorOpener>(out var big))
        {
            big.gameObject.SetActive(true); // this door is so~ unique
            big.transform.parent.GetChild(3).gameObject.SetActive(true);
            return;
        }
    }

    #endregion
    #region harmony

    /// <summary> Informs all the client that the door is open. </summary>
    public void SendDoorOpen(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) throw new System.Exception("Door index is -1 for " + obj.name);

        // write door index to send to clients
        byte[] data = Writer.Write(w => w.Int(index));

        // notify each client that the door has opened so they don't get stuck in a room
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.OpenDoor));
    }

    /// <summary> Checks if the door should be open and opens if it should. </summary>
    public void CheckDoorOpen(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) throw new System.Exception("Door index is -1 for " + obj.name);

        // if the door is marked as open, then it must be reopened
        if (opened.Contains(index)) OpenDoor(obj);
    }

    #endregion
}