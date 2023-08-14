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
    /// <summary> Names of acid levels at level 3-1. </summary>
    public static List<string> AcidLevelsNames = new();
    /// <summary> Names of skull cases at level 5-1. </summary>
    public static List<string> SkullCasesNames = new();

    /// <summary> List of all doors in the level, updated when a level is loaded. </summary>
    private List<GameObject> doors = new();
    /// <summary> List of open doors, cleared only when the player enters a new level. </summary>
    private List<int> opened = new();

    /// <summary> Whether the wall is broken in the arena at level 4-4. </summary>
    private bool IsWallBrokenOn4_4;
    /// <summary> Whether V2 is dead? Yes, it's fucking dead. </summary>
    private bool IsV2Dead = true;
    /// <summary> Whether the exit building is raised at level 4-4. </summary>
    private bool IsExitBuildingRaised;

    /// <summary> Whether the first metro door is open at level 5-1. </summary>
    private bool IsFirstMetroDoorOpen;
    /// <summary> Whether the second metro door is open at level 5-1. </summary>
    private bool IsSecondMetroDoorOpen;

    /// <summary> Whether Minos is dead. </summary>
    private bool IsMinosDead;
    /// <summary> Whether Sisyphus is dead. </summary>
    private bool IsSisyphusDead;

    /// <summary> Name of the last loaded scene. </summary>
    private string LastScene;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("World", Plugin.Instance.transform).AddComponent<World>();

        // updates the list of objects in the level when the scene is loaded
        SceneManager.sceneLoaded += (scene, mode) => Instance.Recache();

        // perfect naming
        AcidLevelsNames = new(new[] { "Door Opener Big", "Door Opener Big 2", "Door Opener (1)" });
        SkullCasesNames = new(new[] { "SkullCase", "SkullCase (1)", "SkullCase (2)" });
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

        // level 3-1 has acid that comes down in layers
        if (SceneHelper.CurrentScene == "Level 3-1")
            foreach (var door in Resources.FindObjectsOfTypeAll<DoorOpener>())
                if (AcidLevelsNames.Contains(door.name)) doors.Add(door.gameObject);

        // level 5-1 has cases with skulls inside them
        if (SceneHelper.CurrentScene == "Level 5-1")
            foreach (var door in Resources.FindObjectsOfTypeAll<DoorOpener>())
                if (SkullCasesNames.Contains(door.transform.parent.gameObject.name)) doors.Add(door.gameObject);

        // there is a door in the arena through which V2 escapes
        if (SceneHelper.CurrentScene == "Level 4-4")
        {
            if (LobbyController.IsOwner)
            {
                // add a listener to notify clients to break the wall/start the outro/raise the exit
                Redirect(BrokenWall(), PacketType.BreakeWall);
                Redirect(V2Outro(), PacketType.StartV2Outro);
                Redirect(ExitBuilding(), PacketType.RaiseExitBuilding);
            }
            else
            {
                // break the wall/start the outro/raise the exit if you have already received a notification
                if (IsWallBrokenOn4_4) BreakWall();
                if (IsV2Dead) StartV2Outro();
                if (IsExitBuildingRaised) RaiseExitBuilding();
            }
        }

        // TODO clear copy-pasted code
        if (SceneHelper.CurrentScene == "Level 5-1")
        {
            if (LobbyController.IsOwner)
            {
                Redirect(MetroDoorActivator(false), PacketType.OpenMetroDoor1);
                Redirect(MetroDoorActivator(true), PacketType.OpenMetroDoor2);
            }
            else
            {
                if (IsFirstMetroDoorOpen) OpenFirstMetroDoor();
                if (IsSecondMetroDoorOpen) OpenSecondMetroDoor();
            }
        }

        // Minos & Sisyphus has a unique cutscene and a non-working exit from the level
        if (SceneHelper.CurrentScene == "Level P-1")
        {
            if (LobbyController.IsOwner)
            {
                Redirect(MinosIntro(), PacketType.StartMinosIntro);
                Redirect(MinosExit(), PacketType.OpenMinosExit);
            }
            else if (IsMinosDead) OpenMinosExit();
        }
        if (SceneHelper.CurrentScene == "Level P-2")
        {
            if (LobbyController.IsOwner)
            {
                Redirect(SisyphusIntro(), PacketType.StartSisyphusIntro);
                Redirect(SisyphusExit(), PacketType.OpenSisyphusExit);
            }
            else if (IsSisyphusDead) OpenSisyphusExit();
        }

        // clear the list of open doors if the player has entered a new level
        if (SceneHelper.CurrentScene != LastScene)
        {
            LastScene = SceneHelper.CurrentScene;

            opened.Clear();
            IsWallBrokenOn4_4 = IsV2Dead = IsExitBuildingRaised = IsMinosDead = false;
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

        // level 3-1 has acid that comes down in layers
        if (obj.TryGetComponent<DoorOpener>(out var acid))
        {
            acid.gameObject.SetActive(true);
            return;
        }
    }

    #endregion
    #region 4-4

    /// <summary> Finds an object activator with the given name and active parent. </summary>
    public ObjectActivator Activator(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<ObjectActivator>();
        return Array.Find(all, a => a.name == name && a.transform.parent.gameObject.activeInHierarchy);
    }

    public ObjectActivator MetroDoorActivator(bool second)
    {
        string name = second ? "MetroBlockDoor (1)" : "MetroBlockDoor";

        var all = Resources.FindObjectsOfTypeAll<ObjectActivator>();
        return Array.Find(all, a => a.transform.parent?.gameObject.name == name && a.transform.parent.gameObject.activeInHierarchy);
    }

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

        wall?.transform.parent.gameObject.SetActive(true);
        wall?.transform.parent.parent.Find("Wall").gameObject.SetActive(false);
    }

    /// <summary> Finds V2 outro activator on level 4-4. </summary>
    public ObjectActivator V2Outro() => Activator("BossOutro");

    /// <summary> Starts V2 outro and loading to the next part of the level. </summary>
    public void StartV2Outro() => V2Outro()?.gameObject.SetActive(IsV2Dead = true);

    /// <summary> Finds exit building activator on level 4-4. </summary>
    public ObjectActivator ExitBuilding() => Activator("ExitBuilding Raise");

    /// <summary> Raises the exit from the level from under the sand. </summary>
    public void RaiseExitBuilding()
    {
        IsExitBuildingRaised = true;
        var exit = ExitBuilding();

        if (exit == null) return;
        exit.gameObject.SetActive(true);

        var bulding = exit.transform.parent.Find("ExitBuilding");
        bulding.GetComponent<Door>().Close();
        bulding.GetChild(14).gameObject.SetActive(true);
    }

    public void OpenFirstMetroDoor() => MetroDoorActivator(false).gameObject.SetActive(IsFirstMetroDoorOpen = true);

    public void OpenSecondMetroDoor() => MetroDoorActivator(true).gameObject.SetActive(IsSecondMetroDoorOpen = true);

    /// <summary> Finds Minos intro activator on level P-1. </summary>
    public ObjectActivator MinosIntro() => Activator("MinosPrimeIntro");

    /// <summary> Starts minos intro. </summary>
    public void StartMinosIntro() => MinosIntro()?.gameObject.SetActive(true);

    /// <summary> Finds exit activator on level P-1. </summary>
    public ObjectActivator MinosExit() => Activator("End");

    /// <summary> Open the exit from the level P-1. </summary>
    public void OpenMinosExit()
    {
        IsMinosDead = true;
        var exit = MinosExit();

        exit?.gameObject.SetActive(true);
        exit?.transform.parent.GetChild(7).gameObject.SetActive(false);
    }

    /// <summary> Finds Sisyphus intro activator on level P-2. </summary>
    public ObjectActivator SisyphusIntro() => Activator("PrimeIntro");

    /// <summary> Starts Sisyphus intro. </summary>
    public void StartSisyphusIntro() => SisyphusIntro()?.gameObject.SetActive(true);

    /// <summary> Finds exit activator on level P-2. </summary>
    public ObjectActivator SisyphusExit() => Activator("Outro");

    /// <summary> Open the exit from the level P-2. </summary>
    public void OpenSisyphusExit()
    {
        IsSisyphusDead = true;
        var exit = SisyphusExit();

        exit?.gameObject.SetActive(true);
        exit?.transform.parent.GetChild(7).gameObject.SetActive(false);
    }

    #endregion
    #region harmony

    /// <summary> Informs all the client that the door is open. </summary>
    public void SendDoorOpen(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) throw new Exception("Door index is -1 for " + obj.name);

        // write door index to send to clients
        byte[] data = Writer.Write(w => w.Int(index));

        // notify each client that the door has opened so they don't get stuck in a room
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.OpenDoor));
    }

    /// <summary> Checks if the door should be open and opens if it should. </summary>
    public void CheckDoorOpen(GameObject obj)
    {
        int index = doors.IndexOf(obj);
        if (index == -1) throw new Exception("Door index is -1 for " + obj.name);

        // if the door is marked as open, then it must be reopened
        if (opened.Contains(index)) OpenDoor(obj);
    }

    #endregion
}