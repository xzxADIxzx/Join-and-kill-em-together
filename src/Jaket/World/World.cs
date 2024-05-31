namespace Jaket.World;

using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

using Version = Version;

/// <summary> Class that manages objects in the level, such as hook points, skull cases, triggers and etc. </summary>
public class World
{
    /// <summary> List of most actions with the world. </summary>
    public static List<WorldAction> Actions = new();
    /// <summary> List of activated actions. Cleared only when the host loads a new level. </summary>
    public static List<byte> Activated = new();

    /// <summary> List of all hook points on the level. </summary>
    public static List<HookPoint> HookPoints = new();
    /// <summary> Last hook point, whose state was synchronized. </summary>
    public static HookPoint LastSyncedPoint;

    /// <summary> List of all tram controllers on the level. </summary>
    public static List<TramControl> Trams = new();
    /// <summary> Trolley with a teleport from the tunnel at level 7-1. </summary>
    public static Transform TunnelRoomba;

    public static Hand Hand;
    public static Leviathan Leviathan;
    public static Minotaur Minotaur;
    public static SecuritySystem[] SecuritySystem = new SecuritySystem[7];
    public static Brain Brain;

    /// <summary> Creates a singleton of world. </summary>
    public static void Load()
    {
        Events.OnLoaded += () =>
        {
            if (LobbyController.Offline) return;
            if (LobbyController.IsOwner)
            {
                Activated.Clear();
                Networking.Send(PacketType.Level, World.WriteData);
            }
            Restore();
        };
        Events.OnLobbyEntered += Restore;
        Events.EveryDozen += Optimize;
    }

    #region data

    /// <summary> Writes data about the world such as level, difficulty and triggers fired. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Tools.Scene);

        // the version is needed for a warning about incompatibility
        w.String(Version.CURRENT);
        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));

        // synchronize activated actions
        w.Bytes(Activated.ToArray());
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and fires triggers. </summary>
    public static void ReadData(Reader r)
    {
        Tools.Load(r.String());
        Networking.Loading = true;

        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.String() != Version.CURRENT)
        {
            Version.Notify();
            return;
        }
        PrefsManager.Instance.SetInt("difficulty", r.Byte());

        Activated.Clear();
        Activated.AddRange(r.Bytes(r.Length - r.Position));
    }

    #endregion
    #region iteration

    /// <summary> Iterates each static world action. </summary>
    public static void EachStatic(Action<StaticAction> cons) => Actions.ForEach(action =>
    {
        if (action is StaticAction sa) cons(sa);
    });

    /// <summary> Iterates each net world action. </summary>
    public static void EachNet(Action<NetAction> cons) => Actions.ForEach(action =>
    {
        if (action is NetAction sa) cons(sa);
    });

    #endregion
    #region general

    /// <summary> Restores activated actions after restart of the level. </summary>
    public static void Restore()
    {
        EachStatic(sa => sa.Run());
        Activated.ForEach(index => Actions[index].Run());

        // change the layer from PlayerOnly to Invisible so that other players will be able to launch the wave
        foreach (var trigger in Tools.ResFind<ActivateArena>()) trigger.gameObject.layer = 16;

        // raise the activation trigger so that players don't get stuck on the sides
        var act = Tools.ObjFind<PlayerActivator>();
        if (act) act.transform.position += Vector3.up * 6f;

        #region trams

        void Find<T>(List<T> list) where T : MonoBehaviour
        {
            list.Clear();
            Tools.ResFind<T>(Tools.IsReal, list.Add);

            // sort the objects by the distance so that their order will be the same for all clients
            list.Sort((t1, t2) => t1.transform.position.sqrMagnitude.CompareTo(t2.transform.position.sqrMagnitude));
        }
        Find(Trams);

        #endregion
        #region hook points

        void Sync(HookPoint point, bool hooked)
        {
            var index = (byte)HookPoints.IndexOf(point);
            if (index != 255 && point != LastSyncedPoint && (point.transform.position - HookArm.Instance.hook.position).sqrMagnitude < 81f)
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

    /// <summary> Optimizes the level by destroying the corpses of enemies. </summary>
    public static void Optimize()
    {
        if (LobbyController.Offline) return;

        bool cg = Tools.Scene == "Endless";
        bool FarEnough(Transform t) => (t.position - NewMovement.Instance.transform.position).sqrMagnitude > 10000f || cg;

        // clear gore zones located further than 100 units from the player
        Tools.ResFind<GoreZone>(zone => Tools.IsReal(zone) && zone.isActiveAndEnabled && FarEnough(zone.transform), zone => zone.ResetGibs());

        // big pieces of corpses, such as arms or legs, are part of the entities
        Networking.Entities.Values.DoIf(entity =>

                entity && entity.Dead && entity is Enemy &&
                entity.Type != EntityType.MaliciousFace &&
                entity.Type != EntityType.Gutterman &&
                entity.LastUpdate < Time.time - 1f &&
                FarEnough(entity.transform),

        entity => entity.gameObject.SetActive(false));
    }

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
