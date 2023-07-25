namespace Jaket.Net;

using System;
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
    /// <summary> Whether the wall is broken in the arena at level 4-4. </summary>
    private bool IsWallBrokenOn4_4;

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
        // find all the doors on the level, because the old ones have already been destroyed
        doors.Clear();

        // FindGameObjectsWithTag may be faster, but doesn't work with inactive objects
        foreach (var door in Resources.FindObjectsOfTypeAll<Door>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<FinalDoor>()) doors.Add(door.gameObject);
        foreach (var door in Resources.FindObjectsOfTypeAll<BigDoorOpener>()) doors.Add(door.gameObject);

        // sort doors by position to make sure their order is the same for different clients
        doors.Sort((d1, d2) => d1.transform.position.sqrMagnitude.CompareTo(d2.transform.position.sqrMagnitude));

        // there is a door in the arena through which V2 escapes
        if (SceneHelper.CurrentScene == "Level 4-4")
        {
            if (LobbyController.IsOwner)
            {
                // add a listener to notify clients to break the wall
                Redirect(BrokenWall(), PacketType.BreakeWall);
                // and a listener to notify clients to start outro
                Redirect(V2Outro(), PacketType.StartV2Outro);
            }
            else
                // or break the wall if you have already received a notification
                if (IsWallBrokenOn4_4) BreakWall();
        }

        // clear the list of open doors if the player has entered a new level
        if (SceneHelper.CurrentScene != LastScene)
        {
            LastScene = SceneHelper.CurrentScene;

            opened.Clear();
            IsWallBrokenOn4_4 = false;
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
    #region 4-4

    /// <summary> Adds a listener to the activator and sends packets to all clients when the listener fires. </summary>
    public void Redirect(ObjectActivator activator, PacketType packetType) =>
        activator?.events.onActivate.AddListener(() => LobbyController.EachMemberExceptOwner(member => Networking.SendEmpty(member.Id, packetType)));

    /// <summary> Finds broken wall activator on level 4-4. </summary>
    public ObjectActivator BrokenWall()
    {
        var all = Resources.FindObjectsOfTypeAll<ObjectActivator>();
        return Array.Find(all, a => a.name == "Checkpoint Activator" && a.transform.parent.parent.gameObject.activeInHierarchy);
    }

    /// <summary> Activate the broken wall and deactivate the old one. </summary>
    public void BreakWall()
    {
        IsWallBrokenOn4_4 = true; // save the state of the wall
        var wall = BrokenWall();

        wall.transform.parent.gameObject.SetActive(true);
        wall.transform.parent.parent.Find("Wall").gameObject.SetActive(false);
    }

    /// <summary> Finds V2 outro activator on level 4-4. </summary>
    public ObjectActivator V2Outro()
    {
        var all = Resources.FindObjectsOfTypeAll<ObjectActivator>();
        return Array.Find(all, a => a.name == "BossOutro" && a.transform.parent.gameObject.activeInHierarchy);
    }

    /// <summary> Starts V2 outro and loading to the next part of the level. </summary>
    public void StartV2Outro() => V2Outro().gameObject.SetActive(true);

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