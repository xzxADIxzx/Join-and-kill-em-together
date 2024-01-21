namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;

/// <summary> Class that manages objects in the level, such as skull cases, rooms and etc. </summary>
public class World : MonoSingleton<World>
{
    /// <summary> List of all possible actions with the world. </summary>
    public static List<WorldAction> Actions = new();
    /// <summary> List of activated actions, cleared only when the host loads a new level. </summary>
    private List<byte> Activated = new();

    /// <summary> There is no prefab for the mini-boss at levels 2-4. </summary>
    public Hand Hand;
    /// <summary> Level 5-4 contains a unique boss that needs to be dealt with separately. </summary>
    public Leviathan Leviathan;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        UI.Object("World").AddComponent<World>();

        Events.OnLoaded += () =>
        {
            foreach (var door in Resources.FindObjectsOfTypeAll<Door>())
            {
                if (door.gameObject.scene == null) return;
                foreach (var room in door.deactivatedRooms) RoomController.Build(room.transform);
            }

            // change the layer from PlayerOnly to Invisible so that other players can also launch a wave
            foreach (var trigger in Resources.FindObjectsOfTypeAll<ActivateArena>())
                trigger.gameObject.layer = 16;

            if (LobbyController.Lobby != null) Instance.Restore();
        };
        Events.OnLobbyEntered += () => Instance.Restore();

        // create world actions to synchronize different things in the level
        Actions.AddRange(new WorldAction[]
        {
            // duplicate torches at levels 4-3 and P-1
            StaticAction.PlaceTorches("Level 4-3", new(0f, -10f, 310f), 3f),
            StaticAction.PlaceTorches("Level P-1", new(-0.84f, -10f, 16.4f), 2f),

            // launching the Minos boss fight unloads some of the locations, which is undesirable
            StaticAction.Find("Level 2-4", "DoorsActivator", new(425f, -10f, 650f), obj =>
            {
                var objs = obj.GetComponent<ObjectActivator>().events.toDisActivateObjects;
                objs[1] = objs[2] = null;
            }),
            // for some reason this object cannot be found located
            StaticAction.Find("Level 5-2", "6 (Secret)", new(-3.5f, -3f, 940.5f), obj =>
            {
                Object.Destroy(obj.transform.Find("Altar (Blue Skull) Variant").GetChild(0).gameObject);
            }),

            // enable arenas that are disabled by default
            StaticAction.Enable("Level 4-2", "6A - Indoor Garden", new(-19f, 35f, 953.9481f)),
            StaticAction.Enable("Level 4-2", "6B - Outdoor Arena", new(35f, 35f, 954f)),

            // destroy objects in any way interfering with multiplayer
            StaticAction.Destroy("Level 2-3", "4 & 5 Fake", new(-26f, 12.5f, 375f)),
            StaticAction.Destroy("Level 2-4", "Doorway Blockers", new(425f, -10f, 650f)),
            StaticAction.Destroy("Level 2-4", "MetroBlockDoor (1)", new(425f, 27f, 615f)),
            StaticAction.Destroy("Level 2-4", "MetroBlockDoor (2)", new(425f, 27f, 525f)),
            StaticAction.Destroy("Level 4-2", "6A Activator", new(-79f, 45f, 954f)),
            StaticAction.Destroy("Level 4-2", "6B Activator", new(116f, 19.5f, 954f)),
            StaticAction.Destroy("Level 5-1", "HudMessage", new(0f, -100f, 295.5f)),
            StaticAction.Destroy("Level 5-1", "Door", new(218.5f, -41f, 234.5f)),
            StaticAction.Destroy("Level 5-2", "Arena 1", new(87.5f, -53f, 1240f)),
            StaticAction.Destroy("Level 5-2", "Arena 2", new(87.5f, -53f, 1240f)),
            StaticAction.Destroy("Level 6-1", "Cage", new(168.5f, -130f, 140f)),
            StaticAction.Destroy("Level 6-1", "Cube", new(102f, -165f, -503f)),

            // there are just a couple of little things that need to be synchronized
            NetAction.Sync("Level 4-2", "DoorOpeners", new(-1.5f, -18f, 774.5f)),
            NetAction.Sync("Level 4-2", "DoorsOpener", new(40f, 5f, 813.5f)),

            // there is a door in the arena through which V2 escapes and you also need to synchronize the outro and the exit building
            NetAction.Sync("Level 4-4", "Checkpoint Activator", new(177.5f, 663.5f, 243f), obj =>
            {
                obj.transform.parent.gameObject.SetActive(true);
                obj.transform.parent.parent.Find("Wall").gameObject.SetActive(false);
            }),
            NetAction.Sync("Level 4-4", "BossOutro", new(117.5f, 663.5f, 323f)),
            NetAction.Sync("Level 4-4", "ExitBuilding Raise", new(1027f, 261f, 202.5f), obj =>
            {
                obj.SetActive(true);

                var exit = obj.transform.parent.Find("ExitBuilding");
                exit.GetComponent<Door>().Close();
                exit.Find("GrapplePoint (2)").gameObject.SetActive(true);
            }),

            // there is a checkpoint deactivator, the deactivation of which needs to be synchronized, and some metro doors
            NetAction.Sync("Level 5-1", "CheckPointsUndisabler", new(0f, -50f, 350f)),
            NetAction.Sync("Level 5-1", "DelayedActivator", new(-15f, 36f, 698f)),
            NetAction.Sync("Level 5-1", "DelayedActivator", new(-15f, 38f, 778f)),

            // Minos & Sisyphus have unique cutscenes and non-functional level exits
            NetAction.Sync("Level P-1", "MinosPrimeIntro", new(405f, -598.5f, 110f)),
            NetAction.Sync("Level P-1", "End", new(405f, -598.5f, 110f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.Find("Cube (2)").gameObject.SetActive(false);

                GameObject.Find("Music 3").SetActive(false);
                obj.transform.parent.Find("Lights").gameObject.SetActive(false);

                StatsManager.Instance.StopTimer();
            }),
            NetAction.Sync("Level P-2", "PrimeIntro", new(-102f, -61.25f, -450f)),
            NetAction.Sync("Level P-2", "Outro", new(-102f, -61.25f, -450f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.Find("Backwall").gameObject.SetActive(false);

                GameObject.Find("BossMusics/Sisyphus").SetActive(false);
                GameObject.Find("IntroObjects/Decorations").SetActive(false);
                GameObject.Find("Rain").SetActive(false);

                StatsManager.Instance.StopTimer();
            }),
        });
    }

    #region data

    /// <summary> Writes data about the world such as level, difficulty and, in the future, triggers fired. </summary>
    public void WriteData(Writer w)
    {
        // write the scene at the very beginning for compatibility with earlier versions
        w.String(SceneHelper.CurrentScene);

        // the version is needed for a warning about incompatibility, and the difficulty is mainly needed for ultrapain
        w.String(Version.CURRENT);
        w.Int(PrefsManager.Instance.GetInt("difficulty"));

        // synchronize the Ultrapain difficulty
        w.Bool(Plugin.UltrapainLoaded);
        if (Plugin.UltrapainLoaded) Plugin.WritePain(w);
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and, in the future, fires triggers. </summary>
    public void ReadData(Reader r)
    {
        // the host may have restarted the same level, so the triggers have to be reset
        // Instance.Clear();

        // reset all of the activated actions
        Activated.Clear();
        // load the host level, it is the main function of this packet
        SceneHelper.LoadScene(r.String());

        // if the data in the packet has run out, it means the host has a version of the mod before the “New Era of Communication” update
        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.Position == r.Length || r.String() != Version.CURRENT) // TODO outdated because the way information is transmitted has changed
        {
            Version.NotifyHost();
            return;
        }

        PrefsManager.Instance.SetInt("difficulty", r.Int());

        if (r.Bool())
        {
            // synchronize different values needed for Ultrapain to work
            if (Plugin.UltrapainLoaded) Plugin.TogglePain(r.Bool(), r.Bool());
            // or skip the values if the mod isn't installed locally
            else r.Inc(2);
        }
    }

    #endregion
    #region iteration

    /// <summary> Iterates each world action and restores it as needed. </summary>
    public void Restore()
    {
        EachStatic(sa => sa.Run());
        Activated.ForEach(index => Actions[index].Run());
    }

    /// <summary> Iterates each static world action. </summary>
    public static void EachStatic(System.Action<StaticAction> cons) => Actions.ForEach(action =>
    {
        if (action is StaticAction sa) cons(sa);
    });

    /// <summary> Iterates each net world action. </summary>
    public static void EachNet(System.Action<NetAction> cons) => Actions.ForEach(action =>
    {
        if (action is NetAction sa) cons(sa);
    });

    #endregion
    #region networking

    /// <summary> Reads the world action and activates it. </summary>
    public void ReadAction(Reader r)
    {
        void Find<T>(Vector3 pos, System.Action<T> cons) where T : Component
        { foreach (var door in Resources.FindObjectsOfTypeAll<T>()) if (door.transform.position == pos) cons(door); }

        switch (r.Byte())
        {
            case 0:
                byte index = r.Byte();
                if (Actions[index] is NetAction na)
                {
                    Activated.Add(index);
                    na.Run();
                }
                break;

            case 1: Find<FinalDoor>(r.Vector(), d => d.transform.Find("FinalDoorOpener").gameObject.SetActive(true)); break;
            case 2: Find<Door>(r.Vector(), d => d.Open()); break;
        }
    }

    /// <summary> Synchronizes network action activation. </summary>
    public static void SyncActivation(NetAction action) => Networking.Send(Content.PacketType.ActivateObject, w =>
    {
        w.Byte(0);
        w.Byte((byte)Actions.IndexOf(action));
    }, size: 2);


    /// <summary> Synchronizes final door or skull case state. </summary>
    public static void SyncOpening(Component door, bool final = true) => Networking.Send(Content.PacketType.ActivateObject, w =>
    {
        w.Byte((byte)(final ? 1 : 2));
        w.Vector(door.transform.position);
    }, size: 13);

    #endregion
}
