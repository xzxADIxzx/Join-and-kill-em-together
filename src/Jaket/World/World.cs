namespace Jaket.World;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> Class that manages objects in the level, such as hook points, skull cases, triggers and etc. </summary>
public class World
{
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

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLoadingStart += () =>
        {
            if (LobbyController.Online && LobbyController.IsOwner && Pending != "Main Menu")
            {
                Activated.Clear();
                Networking.Send(PacketType.Level, DataSize(), WriteData);
            }
        };
        Events.OnLoad += () =>
        {
            if (LobbyController.Online) Restore();
        };
        Events.OnLobbyEnter += Restore;
        Events.EveryDozen += Optimize;
    }

    #region data

    /// <summary> Returns the number of bytes required to write world data. </summary>
    public static int DataSize() => 3 + ((Pending ?? Scene).Length + Version.CURRENT.Length) * 2;

    /// <summary> Writes data about the world such as level, difficulty and triggers fired. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Pending ?? Scene);

        // the version is needed for a warning about incompatibility
        w.String(Version.CURRENT);
        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));

        // synchronize activated actions
        w.Bytes(Activated.ToArray());
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and fires triggers. </summary>
    public static void ReadData(Reader r)
    {
        LoadScn(r.String());

        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.String() != Version.CURRENT)
        {
            Bundle.Hud2NS("version.host-outdated");
            return;
        }
        PrefsManager.Instance.SetInt("difficulty", r.Byte());

        // TODO add the amount of data to the list of args
        // TODO also replace it with a list of bools to avoid collisions
        // Activated.Clear();
        // Activated.AddRange(r.Bytes(r.Length - r.Position));
    }

    #endregion
    #region general

    /// <summary> Restores activated actions after restart of the level. </summary>
    public static void Restore()
    {
        // TODO run static ones
        // TODO restore dynamic ones

        // change the layer from PlayerOnly to Invisible so that other players will be able to launch the wave
        ResFind<ActivateArena>().Each(t => t.gameObject.layer = 16);

        // raise the activation trigger so that players don't get stuck on the sides
        var act = ObjFind<PlayerActivator>();
        if (act) act.transform.position += Vector3.up * 6f;

        #region trams

        void Find<T>(List<T> list) where T : Component
        {
            list.Clear();
            ResFind<T>().Each(IsReal, list.Add);

            // sort the objects by the distance so that their order will be the same for all clients
            list.Sort((t1, t2) => t1.transform.position.sqrMagnitude.CompareTo(t2.transform.position.sqrMagnitude));
        }
        Find(Trams);

        #endregion
        #region hook points

        void Sync(HookPoint point, bool hooked)
        {
            var index = (byte)HookPoints.IndexOf(point);
            if (index != 255 && point != LastSyncedPoint && Within(point, HookArm.Instance.hook, 9f)) SyncAction(index, hooked);
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
        // there is no need to optimize the world if remote entities are not present
        if (LobbyController.Offline) return;

        bool cg = Scene == "Endless";
        bool FarEnough(Transform t) => !Within(t, NewMovement.Instance, 100f) || cg;

        // clear gore zones located further than 100 units from the player
        ResFind<GoreZone>().Each(zone => IsReal(zone) && zone.isActiveAndEnabled && FarEnough(zone.transform), zone => zone.ResetGibs());

        // big pieces of corpses, such as arms or legs, are part of the entities
        // TODO is it even necessary?
    }

    #endregion
    #region networking

    /// <summary> Reads an action with the remote world and applies it to the local one. </summary>
    public static void ReadAction(Reader r)
    {
        void Find<T>(Vector3 pos, Cons<T> cons) where T : Component => ResFind<T>().Each(t => t.transform.position == pos, cons);

        switch (r.Byte())
        {
            case 0:
                byte index = r.Byte();
                // TODO check for level bounds
                // TODO check for active ones
                // TODO run the correspondign action and log abt it
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
            case 7: Find<Flammable>(r.Vector(), d => d.Burn(4.01f)); break;

            case 6:
                // TODO a better way of getting type specific entities (???)
                // Networking.Entities.Alive(entity => entity.Type == EntityType.Puppet, entity => entity.EnemyId.InstaKill());
                Find<BloodFiller>(r.Vector(), f => f.InstaFill());
                break;
        }
    }

    /// <summary> Synchronizes activations of the given game object. </summary>
    // mess, rewrite it
    /*
    public static void SyncAction(GameObject obj) => Actions.Each(a =>
    {
        if (a is not NetAction na) return;
        if (!Within(obj.transform, na.Position) || obj.name != na.Name) return;

        byte index = (byte)Actions.IndexOf(na);
        if (!Activated.Contains(index))
            Networking.Send(PacketType.ActivateObject, 2, w =>
            {
                Activated.Add(index);
                w.Byte(0);
                w.Byte(index);

                Log.Debug($"[World] Sent the activation of the object {na.Name} in {na.Level}");
            });
    });
    */

    /// <summary> Synchronizes the state of a hook point. </summary>
    public static void SyncAction(byte index, bool hooked) => Networking.Send(PacketType.ActivateObject, 3, w =>
    {
        w.Byte(1);
        w.Byte(index);
        w.Bool(hooked);

        Log.Debug($"[World] Sent the new state of point#{index}: {hooked}");
    });

    /// <summary> Synchronizes the tram speed. </summary>
    public static void SyncTram(TramControl tram)
    {
        if (LobbyController.Offline || Scene == "Level 7-1") return;

        var index = (byte)Trams.IndexOf(tram);
        if (index != 255) Networking.Send(PacketType.ActivateObject, 6, w =>
        {
            w.Byte(2);
            w.Byte(index);
            w.Int(tram.currentSpeedStep);

            Log.Debug($"[World] Sent the new speed of tram#{index} {tram.currentSpeedStep}");
        });
    }

    /// <summary> Synchronizes actions characterized only by position: opening doors, activation of a stature or tree. </summary>
    public static void SyncAction(Component t, byte type) => Networking.Send(PacketType.ActivateObject, 13, w =>
    {
        w.Byte(type);
        w.Vector(t.transform.position);
    });

    #endregion
}
