namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> Class that manages objects in the level, such as skull cases, rooms and etc. </summary>
public class World : MonoSingleton<World>
{
    /// <summary> List of all possible actions with the world. </summary>
    public static List<WorldAction> Actions = new();
    /// <summary> List of activated actions, cleared only when the host loads a new level. </summary>
    public List<byte> Activated = new();

    /// <summary> List of all hook points on the level. </summary>
    public List<HookPoint> HookPoints = new();
    /// <summary> Last hook point, whose state was synchronized. </summary>
    public HookPoint LastSyncedPoint;

    /// <summary> List of all tram controllers on the level. </summary>
    public List<TramControl> Trams = new();
    /// <summary> Trolley with a teleport from the tunnel at level 7-1. </summary>
    public Transform TunnelRoomba;

    /// <summary> There is no prefab for the mini-boss at level 2-4. </summary>
    public Hand Hand;
    /// <summary> Level 5-4 contains a unique boss that needs to be dealt with separately. </summary>
    public Leviathan Leviathan;
    /// <summary> The same situation with the Minotaur in the tunnel at level 7-1. </summary>
    public Minotaur Minotaur;
    /// <summary> The security system at level 7-4 consists of several subenemies. </summary>
    public SecuritySystem[] SecuritySystem = new SecuritySystem[7];
    /// <summary> The fight with this boss is special because of the idols. </summary>
    public Brain Brain;

    /// <summary> Creates a singleton of world & listener needed to keep track of objects at the level. </summary>
    public static void Load()
    {
        // initialize the singleton
        Tools.Create<World>("World");

        Events.OnLoaded += () =>
        {
            // change the layer from PlayerOnly to Invisible so that other players will be able to launch the wave
            foreach (var trigger in Tools.ResFind<ActivateArena>()) trigger.gameObject.layer = 16;
            if (LobbyController.Online) Instance.Restore();
        };
        Events.OnLobbyEntered += Instance.Restore;
    }

    #region data

    /// <summary> Writes data about the world such as level, difficulty and, in the future, triggers fired. </summary>
    public void WriteData(Writer w)
    {
        w.String(Tools.Scene);

        // the version is needed for a warning about incompatibility, and the difficulty is mainly needed for ultrapain
        w.String(Version.CURRENT);
        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));

        // synchronize activated actions
        w.Bytes(Activated.ToArray());
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and, in the future, fires triggers. </summary>
    public void ReadData(Reader r)
    {
        // reset all of the activated actions
        Activated.Clear();
        // load the host level, it is the main function of this packet
        Tools.Load(r.String());
        Networking.Loading = true;

        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.String() != Version.CURRENT)
        {
            Version.Notify();
            return;
        }
        PrefsManager.Instance.SetInt("difficulty", r.Byte());

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

        // finds all objects of the given type on the level, store and sort them in the list
        void Find<T>(List<T> list) where T : MonoBehaviour
        {
            list.Clear();
            Tools.ResFind<T>(Tools.IsReal, list.Add);

            // sort the objects by the distance so that their order will be the same for all clients
            list.Sort((t1, t2) => t1.transform.position.sqrMagnitude.CompareTo(t2.transform.position.sqrMagnitude));
        }
        Find(Trams);

        #region hook points

        void Sync(HookPoint point, bool hooked)
        {
            byte index = (byte)HookPoints.IndexOf(point);
            if (Vector3.Distance(point.transform.position, HookArm.Instance.hook.position) < 9f && index != 255 && point != LastSyncedPoint)
                Networking.Send(PacketType.ActivateObject, w =>
                {
                    w.Byte(5);
                    w.Byte(index);
                    w.Bool(hooked);

                    Log.Debug($"[World] Sent new hook state: point#{index} is {hooked}");
                });
        }

        Find(HookPoints);
        HookPoints.ForEach(p =>
        {
            p.onHook.onActivate.AddListener(() => Sync(p, true));
            p.onUnhook.onActivate.AddListener(() => Sync(p, false));
        });

        #endregion
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

            case 5:
                byte indexp = r.Byte();
                bool hooked = r.Bool();
                Log.Debug($"[World] Read a new state of point#{indexp}: {hooked}");

                LastSyncedPoint = HookPoints[indexp];
                if (hooked)
                    LastSyncedPoint.Hooked();
                else
                {
                    LastSyncedPoint.Unhooked();
                    if (LastSyncedPoint.type == hookPointType.Switch) LastSyncedPoint.SwitchPulled();
                }
                break;

            case 6:
                byte indext = r.Byte();
                int speed = r.Int();
                Log.Debug($"[World] Read a new speed of tram#{indext}: {speed}");

                Trams[indext].currentSpeedStep = speed;
                break;

            case 1: Find<FinalDoor>(r.Vector(), d => d.transform.Find("FinalDoorOpener").gameObject.SetActive(true)); break;
            case 2: Find<Door>(r.Vector(), d => d.Open()); break;

            case 3:
                Networking.EachEntity(entity =>
                {
                    if (entity.Type == EntityType.Swordsmachine) entity.Kill(null);
                });
                break;

            case 4:
                Networking.EachEntity(entity =>
                {
                    if (entity.Type == EntityType.Puppet) entity.Kill(null);
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
            Log.Debug($"[World] Sent the activation of the object {action.Name} in {action.Level}");
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

    /// <summary> Synchronizes the tram speed. </summary>
    public static void SyncTram(TramControl tram)
    {
        if (LobbyController.Offline || Tools.Scene == "Level 7-1") return;

        byte index = (byte)Instance.Trams.IndexOf(tram);
        if (index != 255) Networking.Send(PacketType.ActivateObject, w =>
        {
            w.Byte(6);
            w.Byte(index);
            w.Int(tram.currentSpeedStep);

            Log.Debug($"[World] Sent the tram speed: tram#{index} {tram.currentSpeedStep}");
        }, size: 8);
    }

    #endregion
}
