namespace Jaket.Content;

using Steamworks;
using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;
using Jaket.UI;

/// <summary> List of all bullets in the game and some useful methods. </summary>
public class Bullets
{
    /// <summary> List of all bullets in the game. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> These objects are used as damage conventions. </summary>
    public static GameObject SynchronizedBullet, NetworkDamage;

    /// <summary> List of melee weapons and projectile with enemy, as it can be reflected, which is considered melee. </summary>
    public static List<string> Melee = new(new[] { "punch", "heavypunch", "ground slam", "projectile", "enemy" });

    /// <summary> Loads all bullets for future use. </summary>
    public static void Load()
    {
        var all = Weapons.Prefabs;
        foreach (var weapon in all)
        {
            if (weapon.TryGetComponent<Revolver>(out var revolver))
            {
                Prefabs.Add(revolver.revolverBeam);
                Prefabs.Add(revolver.revolverBeamSuper);
                Prefabs.Add(revolver.coin); // a coin is also a bullet, honestly-honestly
                continue;
            }

            if (weapon.TryGetComponent<Shotgun>(out var shotgun))
            {
                Prefabs.Add(shotgun.bullet);
                Prefabs.Add(shotgun.grenade);
                Prefabs.Add(shotgun.explosion);
                continue;
            }

            if (weapon.TryGetComponent<Nailgun>(out var nailgun))
            {
                Prefabs.Add(nailgun.nail);
                Prefabs.Add(nailgun.heatedNail);
                Prefabs.Add(nailgun.magnetNail);
                continue;
            }

            if (weapon.TryGetComponent<Railcannon>(out var railcannon))
            {
                Prefabs.Add(railcannon.beam);
                continue;
            }

            if (weapon.TryGetComponent<RocketLauncher>(out var launcher))
            {
                Prefabs.Add(launcher.rocket);
                Prefabs.Add(launcher.cannonBall?.gameObject);
                continue;
            }
        }

        // some variants are missing some projectiles
        Prefabs.RemoveAll(bullet => bullet == null);

        // create damage conventions
        SynchronizedBullet = Utils.Object("Synchronized Bullet", Plugin.Instance.transform);
        NetworkDamage = Utils.Object("Network Damage", Plugin.Instance.transform);
    }

    #region index

    /// <summary> Finds enemy index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds enemy index by the name of its clone. </summary>
    public static int CopiedIndex(string name) => Index(name.Substring(0, name.IndexOf("(Clone)")));

    #endregion
    #region serialization

    /// <summary> Writes bullet to the writer. </summary>
    public static void Write(Writer w, GameObject bullet, bool hasRigidbody = false, bool applyOffset = true)
    {
        int index = bullet.name == "ReflectedBeamPoint(Clone)" ? Index("Revolver Beam") : CopiedIndex(bullet.name);
        if (index == -1) return; // there is no sense to synchronize enemy bullets

        w.Int(index);

        w.Vector(bullet.transform.position + (applyOffset ? bullet.transform.forward * 2f : Vector3.zero));
        w.Vector(bullet.transform.eulerAngles);

        w.Bool(hasRigidbody);
        if (hasRigidbody)
        {
            var body = bullet.GetComponent<Rigidbody>();
            w.Vector(body.velocity);
        }
    }

    /// <summary> Writes bullet to the byte array. </summary>
    public static byte[] Write(GameObject bullet, bool hasRigidbody = false, bool applyOffset = true) => Writer.Write(w => Write(w, bullet, hasRigidbody, applyOffset));

    /// <summary> Reads bullet from the reader. </summary>
    public static void Read(Reader r)
    {
        var obj = Object.Instantiate(Prefabs[r.Int()]);
        obj.name = "Net"; // needed to prevent object looping between client and server

        obj.transform.position = r.Vector();
        obj.transform.eulerAngles = r.Vector();

        if (r.Bool()) obj.GetComponent<Rigidbody>().velocity = r.Vector();
    }

    /// <summary> Reads bullet from the byte array. </summary>
    public static void Read(byte[] data) => Reader.Read(data, Read);

    #endregion
    #region harmony

    /// <summary> Sends the bullet to all other players if it is local, otherwise ignore. </summary>
    public static void Send(GameObject bullet, bool hasRigidbody = false, bool applyOffset = true)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || bullet.name.StartsWith("Net") || bullet.name.StartsWith("New")) return;

        // write bullet data to send to server or clients
        byte[] data = Write(bullet, hasRigidbody, applyOffset);

        // if no data was written, then the bullet belongs to an enemy
        if (data.Length != 0) Networking.Redirect(data, PacketType.SpawnBullet);
    }

    /// <summary> Sends the bullet to all other players and also replaces source weapon if it is null. </summary>
    public static void Send(GameObject bullet, ref GameObject sourceWeapon, bool hasRigidbody = false, bool applyOffset = true)
    {
        if (sourceWeapon == null) sourceWeapon = SynchronizedBullet;
        Send(bullet, hasRigidbody, applyOffset);
    }

    /// <summary> Sends the explosion of the knuckleblaster. </summary>
    public static void SendBlast(GameObject blast)
    {
        // checking if this is really a knuckleblaster explosion
        if (LobbyController.Lobby == null || blast?.name != "Explosion Wave(Clone)") return;

        // write blast data to send to server or clients
        Networking.Redirect(Writer.Write(w =>
        {
            w.Id(SteamClient.SteamId);
            w.Byte(1);

            w.Vector(blast.transform.position);
            w.Vector(blast.transform.localEulerAngles);
        }), PacketType.Punch);
    }

    /// <summary> Sends the shockwave to the player. </summary>
    public static void SendShock(GameObject shock, float force)
    {
        // checking if this is really a player's shockwave
        if (LobbyController.Lobby == null || shock?.name != "PhysicalShockwavePlayer(Clone)") return;

        // write shock data to send to server or clients
        Networking.Redirect(Writer.Write(w =>
        {
            w.Id(SteamClient.SteamId);
            w.Byte(2);

            w.Float(force);
        }), PacketType.Punch);
    }

    /// <summary> Deals bullet damage to an enemy. </summary>
    public static void DealDamage(EnemyIdentifier enemyId, Reader r)
    {
        r.Byte(); // skip team because enemies don't have a team
        if (r.Bool()) enemyId.hitter = Melee[0]; // if the damage was caused by a melee, then this must be reported to EnemyIdentifier

        // dealing direct damage
        enemyId.DeliverDamage(enemyId.gameObject, r.Vector(), Vector3.zero, r.Float(), r.Bool(), r.Float(), NetworkDamage);
    }

    #endregion
}