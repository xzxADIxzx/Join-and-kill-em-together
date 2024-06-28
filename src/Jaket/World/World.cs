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
        Events.OnLoadingStarted += () =>
        {
            if (LobbyController.Online && LobbyController.IsOwner && Tools.Pending != "Main Menu")
            {
                Activated.Clear();
                Networking.Send(PacketType.Level, WriteData, size: 256);
            }
        };
        Events.OnLoaded += () =>
        {
            if (LobbyController.Online) Restore();
        };
        Events.OnLobbyEntered += Restore;
        Events.EveryDozen += Optimize;
    }

    #region data

    /// <summary> Writes data about the world such as level, difficulty and triggers fired. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Tools.Pending ?? Tools.Scene);

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
        Events.Post(() =>
        {
            EachStatic(sa => sa.Run());
            Activated.ForEach(index => Actions[index].Run());
        });

        // change the layer from PlayerOnly to Invisible so that other players will be able to launch the wave
        foreach (var trigger in Tools.ResFind<ActivateArena>()) trigger.gameObject.layer = 16;

        // raise the activation trigger so that players don't get stuck on the sides
        var act = Tools.ObjFind<PlayerActivator>();
        if (act) act.transform.position += Vector3.up * 6f;

        #region trams

        void Find<T>(List<T> list) where T : Component
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
            if (index != 255 && point != LastSyncedPoint && Tools.Within(point.transform, HookArm.Instance.hook.position, 9f)) SyncAction(index, hooked);
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
        bool FarEnough(Transform t) => !Tools.Within(t, NewMovement.Instance.transform, 100f) || cg;

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

    /// <summary> Reads an action with the remote world and applies it to the local one. </summary>
    public static void ReadAction(Reader r)
    {
        void Find<T>(Vector3 pos, Action<T> cons) where T : Component => Tools.ResFind(t => t.transform.position == pos, cons);

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

            case 1:
                byte indexp = r.Byte();
                bool hooked = r.Bool();
                Log.Debug($"[World] Read the new state of point#{indexp}: {hooked}");

                LastSyncedPoint = HookPoints[indexp];
                if (hooked)
                    LastSyncedPoint.Hooked();
                else
                {
                    LastSyncedPoint.Unhooked();
                    if (LastSyncedPoint.type == hookPointType.Switch) LastSyncedPoint.SwitchPulled();
                }
                break;

            case 2:
                byte indext = r.Byte();
                int speed = r.Int();
                Log.Debug($"[World] Read the new speed of tram#{indext}: {speed}");

                Trams[indext].currentSpeedStep = speed;
                break;

            case 3: Find<FinalDoor>(r.Vector(), d => d.transform.Find("FinalDoorOpener").gameObject.SetActive(true)); break;
            case 4: Find<Door>(r.Vector(), d => d.Open()); break;

            case 5:
                Find<StatueActivator>(r.Vector(), d =>
                {
                    d.gameObject.SetActive(true);
                    d.transform.parent.gameObject.SetActive(true);
                });
                break;
            case 6:
                Networking.EachEntity(entity => entity.Type == EntityType.Puppet, entity => entity.EnemyId.InstaKill());
                Find<BloodFiller>(r.Vector(), f => f.InstaFill());
                break;
        }
    }

    /// <summary> Synchronizes activations of the given game object. </summary>
    public static void SyncAction(GameObject obj) => EachNet(na =>
    {
        if (!Tools.Within(obj, na.Position) || obj.name != na.Name) return;

        var index = (byte)Actions.IndexOf(na);
        if (LobbyController.IsOwner || !Activated.Contains(index))
            Networking.Send(PacketType.ActivateObject, w =>
            {
                Activated.Add(index);
                w.Byte(0);
                w.Byte(index);

                Log.Debug($"[World] Sent the activation of the object {na.Name} in {na.Level}");
            }, size: 2);
    });

    /// <summary> Synchronizes the state of a hook point. </summary>
    public static void SyncAction(byte index, bool hooked) => Networking.Send(PacketType.ActivateObject, w =>
    {
        w.Byte(1);
        w.Byte(index);
        w.Bool(hooked);

        Log.Debug($"[World] Sent the new state of point#{index}: {hooked}");
    }, size: 3);

    /// <summary> Synchronizes the tram speed. </summary>
    public static void SyncTram(TramControl tram)
    {
        if (LobbyController.Offline || Tools.Scene == "Level 7-1") return;

        var index = (byte)Trams.IndexOf(tram);
        if (index != 255) Networking.Send(PacketType.ActivateObject, w =>
        {
            w.Byte(2);
            w.Byte(index);
            w.Int(tram.currentSpeedStep);

            Log.Debug($"[World] Sent the new speed of tram#{index} {tram.currentSpeedStep}");
        }, size: 6);
    }

    /// <summary> Synchronizes actions characterized only by position: opening doors, activation of a stature or tree. </summary>
    public static void SyncAction(Component t, byte type) => Networking.Send(PacketType.ActivateObject, w =>
    {
        w.Byte(type);
        w.Vector(t.transform.position);
    }, size: 13);

    #endregion
}
