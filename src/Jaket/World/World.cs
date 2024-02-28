namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;
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
    public List<byte> Activated = new();

    /// <summary> There is no prefab for the mini-boss at level 2-4. </summary>
    public Hand Hand;
    /// <summary> Level 5-4 contains a unique boss that needs to be dealt with separately. </summary>
    public Leviathan Leviathan;

    /// <summary> Trolley with a teleport from the tunnel at level 7-1. </summary>
    public Transform TunnelRoomba;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        UI.Object("World").AddComponent<World>();

        Events.OnLoaded += () =>
        {
            Tools.ResFind<Door>(door => door.gameObject.scene != null, door =>
            {
                foreach (var room in door.deactivatedRooms) RoomController.Build(room.transform);
            });

            // change the layer from PlayerOnly to Invisible so that other players can also launch a wave
            foreach (var trigger in Tools.ResFind<ActivateArena>()) trigger.gameObject.layer = 16;

            if (LobbyController.Lobby != null) Instance.Restore();
        };
        Events.OnLobbyEntered += Instance.Restore;

        // create world actions to synchronize different things in the level
        Actions.AddRange(new WorldAction[]
        {
            // duplicate torches at levels 4-3 and P-1
            StaticAction.PlaceTorches("Level 4-3", new(0f, -10f, 310f), 3f),
            StaticAction.PlaceTorches("Level P-1", new(-0.84f, -10f, 16.4f), 2f),

            // disable boss fight launch trigger for clients in order to sync cutscene
            StaticAction.Find("Level 1-4", "Cube", new(0f, 11f, 612f), obj =>
            {
                obj.SetActive(LobbyController.IsOwner);
                Destroy(obj.GetComponent<DoorController>());
            }),
            StaticAction.Find("Level 1-4", "V2 - Arena", new(0f, -17f, 563f), obj => Events.Post2(() =>
            {
                if (!LobbyController.IsOwner) foreach (Transform child in obj.transform) Destroy(child.Find("V2")?.gameObject);
            })),
            // launching the Minos boss fight unloads some of the locations, which is undesirable
            StaticAction.Find("Level 2-4", "DoorsActivator", new(425f, -10f, 650f), obj =>
            {
                var objs = obj.GetComponent<ObjectActivator>().events.toDisActivateObjects;
                objs[1] = objs[2] = null;
            }),
            StaticAction.Find("Level 2-4", "3 - First Encounter", new(0f, -12f, 650f), obj => Events.Post2(() =>
            {
                foreach (Transform child in obj.transform) Destroy(child.Find("Blockers")?.Find("Cube")?.gameObject);
            })),
            // for some reason this object cannot be found located
            StaticAction.Find("Level 5-2", "6 (Secret)", new(-3.5f, -3f, 940.5f), obj =>
            {
                Destroy(obj.transform.Find("Altar (Blue Skull) Variant").GetChild(0).gameObject);
            }),
            // fix the red altar at the very beggining of the level
            StaticAction.Find("Level 7-1", "Cube", new(0f, 3.4f, 582.5f), obj => obj.transform.position = new(0f, 7.4f, 582.5f)),
            // disable the roomba panel for clients
            StaticAction.Find("Level 7-1", "ScreenActivator", new(-242.5f, -112f, 311f), obj =>
            {
                if (!LobbyController.IsOwner) obj.SetActive(false);
            }),
            // boss fight unloads the previous location
            StaticAction.Find("Level 7-1", "FightStart", new(-242.5f, 120f, -399.75f), obj =>
            {
                obj.GetComponent<ObjectActivator>().events.toDisActivateObjects[2] = null;
            }),
            // wtf?! why is there a torch???
            StaticAction.Find("Level 7-3", "1 - Dark Path", new(0f, -10f, 300f), obj =>
            {
                Destroy(obj.transform.Find("Altar (Torch) Variant").GetChild(0).gameObject);
            }),
            // some doors don't want to be opened
            StaticAction.Find("Level 7-3", "Door 1", new(-55.5f, -2.5f, 618.5f), obj => obj.GetComponent<Door>().Unlock()),
            StaticAction.Find("Level 7-3", "Door 2", new(-75.5f, -12.5f, 568.5f), obj => obj.GetComponent<Door>().Unlock()),
            StaticAction.Find("Level 7-3", "Door 1", new(-75.5f, -12.5f, 578.5f), obj => obj.GetComponent<Door>().Unlock()),
            // teleport players to the final room once the door is opened
            StaticAction.Find("Level 7-3", "12 - Grand Hall", new(-212.5f, -35f, 483.75f), obj =>
            {
                obj.GetComponent<ObjectActivator>().events.onActivate.AddListener(() =>
                {
                    NewMovement.Instance.transform.position = new(-189f, -33.5f, 483.75f); // the position of the closest checkpoint
                });
            }),
            // strange door blocker
            StaticAction.Find("Level 7-3", "ViolenceHallDoor", new(-148f, 7.5f, 276.25f), obj => Destroy(obj.GetComponent<Collider>())),
            // disable door blocker
            StaticAction.Find("Level P-1", "Trigger", new(360f, -568.5f, 110f), obj =>
            {
                obj.GetComponent<ObjectActivator>().events.toActivateObjects[4] = null;
            }),
            // move the death zone, because entities spawn at the origin
            StaticAction.Find("Endless", "Cube", new(-40f, 0.5f, 102.5f), obj => obj.transform.position = new(-40f, -10f, 102.5f)),

            // crutches everywhere, crutches all the time
            StaticAction.Patch("Level 7-1", "Blockers", new(-242.5f, -115f, 314f)),
            StaticAction.Patch("Level 7-1", "Wave 2", new(-242.5f, 0f, 0f)),

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
            StaticAction.Destroy("Level 4-3", "Doorblocker", new(-59.5f, -35f, 676f)),
            StaticAction.Destroy("Level 5-1", "HudMessage", new(0f, -100f, 295.5f)),
            StaticAction.Destroy("Level 5-1", "Door", new(218.5f, -41f, 234.5f)),
            StaticAction.Destroy("Level 5-2", "Arena 1", new(87.5f, -53f, 1240f)),
            StaticAction.Destroy("Level 5-2", "Arena 2", new(87.5f, -53f, 1240f)),
            StaticAction.Destroy("Level 6-1", "Cage", new(168.5f, -130f, 140f)),
            StaticAction.Destroy("Level 6-1", "Cube", new(102f, -165f, -503f)),
            StaticAction.Destroy("Level 7-1", "SkullRed", new(-66.25f, 9.8f, 485f)),
            StaticAction.Destroy("Level 7-1", "ViolenceArenaDoor", new(-120f, 0f, 530.5f)),
            StaticAction.Destroy("Level 7-1", "Walkway Arena -> Stairway Up", new(80f, -25f, 590f)),
            StaticAction.Destroy("Level 7-3", "Door 2", new(-95.5f, 7.5f, 298.75f)),
            StaticAction.Destroy("Level 7-3", "ViolenceHallDoor (1)", new(-188f, 7.5f, 316.25f)),
            StaticAction.Destroy("Level 7-4", "ArenaWalls", new(-26.5f, 470f, 763.75f)),

            // there is a special very big door
            NetAction.Sync("Level 0-5", "DelayedDoorActivation", new(175f, -6f, 382f)),

            // there is a door within the Very Cancerous Rodent
            NetAction.Sync("Level 1-2", "Cube (1)", new(-61f, -21.5f, 400.5f)),

            // different things related to the boss
            NetAction.Sync("Level 1-4", "Cube", new(0f, -19f, 612f), obj =>
            {
                obj.SetActive(true);
                obj.GetComponent<ObjectActivator>().Activate();
            }),

            // there is an epic boss fight with The Corpse of King Minos
            NetAction.Sync("Level 2-4", "DeadMinos", new(279.5f, -599f, 575f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.Find("GlobalLights (2)").Find("MetroWall (10)").gameObject.SetActive(false);
                obj.transform.parent.Find("BossMusic").gameObject.SetActive(false);
            }),

            // there are just a couple of little things that need to be synchronized
            NetAction.Sync("Level 4-2", "DoorOpeners", new(-1.5f, -18f, 774.5f)),
            NetAction.Sync("Level 4-2", "DoorsOpener", new(40f, 5f, 813.5f)),

            // there is a secret boss - Mandalore and a single weird door
            NetAction.Sync("Level 4-3", "DoorActivator", new(2.5f, -40f, 628f)),
            NetAction.Sync("Level 4-3", "Trigger (Intro)", new(-104f, -20f, 676f)),
            NetAction.Sync("Level 4-3", "Secret Tablet", new(-116.425f, -39.593f, 675.9866f), obj =>
            {
                obj.SetActive(true);
                MusicManager.Instance.StopMusic();
            }),

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

            // boss fight roomba logic
            NetAction.Sync("Level 7-1", "Blockers", new(-242.5f, -115f, 314f), obj =>
            {
                // enable the level in case the player is somewhere else
                obj.transform.parent.parent.parent.parent.gameObject.SetActive(true);

                var btn = obj.transform.parent.Find("Screen").GetChild(0).GetChild(0).GetChild(0).GetChild(0);
                var pointer = btn.GetComponents<MonoBehaviour>()[2];

                var pressed = Tools.Property("OnPressed", pointer).GetValue(pointer) as UnityEvent;
                pressed.Invoke(); // so much pain over a private class

                // teleport the player to the roomba so that they are not left behind
                NewMovement.Instance.transform.position = obj.transform.position with { y = -112.5f };
            }),
            NetAction.Sync("Level 7-1", "Wave 2", new(-242.5f, 0f, 0f)),
            NetAction.Sync("Level 7-1", "Wave 3", new(-242.5f, 0f, 0f)),
            NetAction.Sync("Level 7-1", "PlayerTeleportActivator", new(-242.5f, 0f, 0f)),

            // cutscene of the falling tower
            NetAction.Sync("Level 7-2", "TowerDestruction", new(-119.75f, 34f, 552.25f)),

            // door lockers
            NetAction.Sync("Level 7-3", "Opener", new(-170.5f, 0.5f, 480.75f)),
            NetAction.Sync("Level 7-3", "Opener", new(-170.5f, 0.5f, 490.75f), obj =>
            {
                obj.SetActive(true);
                Tools.ObjFind("Outdoors Areas/6 - Interior Garden/NightSkyActivator").SetActive(true);
            }),
            NetAction.Sync("Level 7-3", "BigDoorOpener", new(-145.5f, -10f, 483.75f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.gameObject.SetActive(true);
            }),

            // Minos & Sisyphus have unique cutscenes and non-functional level exits
            NetAction.Sync("Level P-1", "MinosPrimeIntro", new(405f, -598.5f, 110f)),
            NetAction.Sync("Level P-1", "End", new(405f, -598.5f, 110f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.Find("Cube (2)").gameObject.SetActive(false);

                Tools.ObjFind("Music 3").SetActive(false);
                obj.transform.parent.Find("Lights").gameObject.SetActive(false);

                StatsManager.Instance.StopTimer();
            }),
            NetAction.Sync("Level P-2", "PrimeIntro", new(-102f, -61.25f, -450f)),
            NetAction.Sync("Level P-2", "Outro", new(-102f, -61.25f, -450f), obj =>
            {
                obj.SetActive(true);
                obj.transform.parent.Find("Backwall").gameObject.SetActive(false);

                Tools.ObjFind("BossMusics/Sisyphus").SetActive(false);
                Tools.ObjFind("IntroObjects/Decorations").SetActive(false);
                Tools.ObjFind("Rain").SetActive(false);

                StatsManager.Instance.StopTimer();
            }),
        });
    }

    #region data

    /// <summary> Writes data about the world such as level, difficulty and, in the future, triggers fired. </summary>
    public void WriteData(Writer w)
    {
        w.String(Tools.Scene);

        // the version is needed for a warning about incompatibility, and the difficulty is mainly needed for ultrapain
        w.String(Version.CURRENT);
        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));

        // synchronize the Ultrapain difficulty
        w.Bool(Plugin.UltrapainLoaded);
        if (Plugin.UltrapainLoaded) Plugin.WritePain(w);

        // synchronize activated actions
        w.Bytes(Activated.ToArray());
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and, in the future, fires triggers. </summary>
    public void ReadData(Reader r)
    {
        // reset all of the activated actions
        Activated.Clear();
        // load the host level, it is the main function of this packet
        SceneHelper.LoadScene(r.String());

        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.String() != Version.CURRENT)
        {
            Version.NotifyHost();
            return;
        }
        PrefsManager.Instance.SetInt("difficulty", r.Byte());

        if (r.Bool())
        {
            // synchronize different values needed for Ultrapain to work
            if (Plugin.UltrapainLoaded) Plugin.TogglePain(r.Bool(), r.Bool());
            // or skip the values if the mod isn't installed locally
            else r.Inc(2);
        }

        Activated.AddRange(r.Bytes(r.Length - r.Position));
    }

    #endregion
    #region iteration

    /// <summary> Iterates each world action and restores it as needed. </summary>
    public void Restore()
    {
        EachStatic(sa => sa.Run());
        Activated.ForEach(index => Actions[index].Run());

        // raise the activation trigger so that the player doesn't get stuck on the sides
        var act = FindObjectOfType<PlayerActivator>();
        if (act) act.transform.position += Vector3.up * 6f;
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
        void Find<T>(Vector3 pos, System.Action<T> cons) where T : Component => Tools.ResFind(door => door.transform.position == pos, cons);

        switch (r.Byte())
        {
            case 0:
                byte index = r.Byte();
                if (Actions[index] is NetAction na)
                {
                    Log.Debug($"[World] Read the activation of the object {na.Name} in {na.Level}");
                    Activated.Add(index);
                    na.Run();
                }
                break;

            case 1: Find<FinalDoor>(r.Vector(), d => d.transform.Find("FinalDoorOpener").gameObject.SetActive(true)); break;
            case 2: Find<Door>(r.Vector(), d => d.Open()); break;

            case 3:
                Networking.EachEntity(entity =>
                {
                    if (entity.Type == EntityType.Swordsmachine) entity.Kill();
                });
                break;

            case 4:
                Networking.EachEntity(entity =>
                {
                    if (entity.Type == EntityType.Puppet) entity.Kill();
                });
                Find<BloodFiller>(r.Vector(), f => f.InstaFill());
                break;
        }
    }

    /// <summary> Synchronizes network action activation. </summary>
    public static void SyncActivation(NetAction action) => Networking.Send(PacketType.ActivateObject, w =>
    {
        byte index = (byte)Actions.IndexOf(action);
        if (index != 0xFF)
        {
            Log.Debug($"[World] Send the activation of the object {action.Name} in {action.Level}");
            Instance.Activated.Add(index);
            w.Byte(0);
            w.Byte(index);
        }
    }, size: 2);

    /// <summary> Synchronizes final door or skull case state. </summary>
    public static void SyncOpening(Component door, bool final = true) => Networking.Send(PacketType.ActivateObject, w =>
    {
        w.Byte((byte)(final ? 1 : 2));
        w.Vector(door.transform.position);
    }, size: 13);

    /// <summary> Synchronizes the drop of a shotgun from Swordsmachine. </summary>
    public static void SyncDrop() => Networking.Send(PacketType.ActivateObject, w => w.Byte(3), size: 1);

    /// <summary> Synchronizes the activation of a tree??? </summary>
    public static void SyncTree(BloodFiller filler) => Networking.Send(PacketType.ActivateObject, w =>
    {
        w.Byte(4);
        w.Vector(filler.transform.position);
    }, size: 13);

    #endregion
}
