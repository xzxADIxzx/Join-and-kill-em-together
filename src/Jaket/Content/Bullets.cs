namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;
using Jaket.UI;

/// <summary> List of all bullets in the game and some useful methods. </summary>
public class Bullets
{
    /// <summary> List of prefabs of all bullets. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Damage markers to prevent synchronized bullets from dealing extra damage. </summary>
    public static GameObject Fake, NetDmg;
    /// <summary> List of all synchronized damage types. </summary>
    public static string[] Types = new[]
    {
        "coin", "revolver", "shotgun", "shotgunzone", "nail", "sawblade", "harpoon", "railcannon", "drill", "cannonball", "explosion", "aftershock",
        "punch", "heavypunch", "ground slam", "hook", "projectile", "enemy"
    };

    /// <summary> Loads all bullets for future use. </summary>
    public static void Load()
    {
        void Add(GameObject bullet, string name)
        {
            if (bullet != null && !Prefabs.Contains(bullet))
            {
                Prefabs.Add(bullet);
                bullet.name = name;
            }
        }
        int rv = 0, ng = 0, rc = 0;
        foreach (var weapon in Weapons.Prefabs)
        {
            if (weapon.TryGetComponent<Revolver>(out var revolver))
            {
                Add(revolver.revolverBeam, $"RV{++rv} PRI");
                Add(revolver.revolverBeamSuper, $"RV{rv} ALT");
                Add(revolver.coin, "Coin"); // a coin is also a bullet, honestly-honestly
            }
            else
            if (weapon.TryGetComponent<Shotgun>(out var shotgun))
            {
                Add(shotgun.bullet, $"SG PRI");
                Add(shotgun.grenade, $"SG ALT");
                Add(shotgun.explosion, $"SG EXT");
            }
            else
            if (weapon.TryGetComponent<Nailgun>(out var nailgun))
            {
                Add(nailgun.nail, $"NG{++ng} PRI");
                Add(nailgun.heatedNail, $"NG{ng} ALT");
                Add(nailgun.magnetNail, $"NG{ng} EXT");
            }
            else
            if (weapon.TryGetComponent<Railcannon>(out var railcannon))
            {
                Add(railcannon.beam, $"RC{++rc} PRI");
            }
            else
            if (weapon.TryGetComponent<RocketLauncher>(out var launcher))
            {
                Add(launcher.rocket, $"RL PRI");
                Add(launcher.cannonBall?.gameObject, $"RL ALT");
            }
        }

        Fake = UI.Object("Fake", Networking.Instance.transform);
        NetDmg = UI.Object("Network Damage", Networking.Instance.transform);
    }

    /// <summary> Finds the bullet type by the name. </summary>
    public static byte CType(string name)
    {
        name = name.Substring(0, name.IndexOf("(Clone)"));
        return (byte)Prefabs.FindIndex(prefab => prefab.name == name);
    }
    public static EntityType EType(string name) => name switch
    {
        "RL PRI" => EntityType.Rocket,
        "RL ALT" => EntityType.Ball,
        _ => EntityType.None
    };

    /// <summary> Spawns a bullet with the given type or other data. </summary>
    public static void CInstantiate(Reader r)
    {
        var obj = Object.Instantiate(Prefabs[r.Byte()]);
        obj.name = "Net"; // these bullets are not entities, so you need to manually change the name

        obj.transform.position = r.Vector();
        obj.transform.eulerAngles = r.Vector();

        if (r.Position < r.Length) obj.GetComponent<Rigidbody>().velocity = r.Vector();
    }

    /// <summary> Synchronizes the bullet between host and clients. </summary>
    public static void Sync(GameObject bullet, bool hasRigidbody, bool applyOffset)
    {
        if (LobbyController.Lobby == null || bullet.name.Contains("Net")) return;

        if (bullet.name != "RL PRI" && bullet.name != "RL ALT")
        {
            var type = CType(bullet.name);
            if (type == 0xFF) return; // how? these are probably enemy projectiles

            Networking.Send(PacketType.SpawnBullet, w =>
            {
                w.Byte(type);
                w.Vector(bullet.transform.position + (applyOffset ? bullet.transform.forward * 2f : Vector3.zero));
                w.Vector(bullet.transform.eulerAngles);

                if (hasRigidbody) w.Vector(bullet.GetComponent<Rigidbody>().velocity);
            }, size: hasRigidbody ? 37 : 25);
        }
        else
        {
            var type = EType(bullet.name);
            if (type == EntityType.None) return;

            Networking.Send(PacketType.SpawnEntity, w =>
            {
                w.Enum(type);
                w.Vector(bullet.transform.position);
            }, size: 13);
        }
    }

    /// <summary> Synchronizes the bullet and marks it as fake if it was downloaded from the network. </summary>
    public static void Sync(GameObject bullet, ref GameObject sourceWeapon, bool hasRigidbody, bool applyOffset)
    {
        if (sourceWeapon == null) sourceWeapon = Fake; // mark synced bullets as fake
        Sync(bullet, hasRigidbody, applyOffset);
    }

    #region special

    /// <summary> Synchronizes the explosion of the knuckleblaster. </summary>
    public static void SyncBlast(GameObject blast)
    {
        // checking if this is really knuckleblaster explosion
        if (LobbyController.Lobby == null || blast?.name != "Explosion Wave(Clone)") return;
        Networking.Send(PacketType.Punch, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Byte(1);

            w.Vector(blast.transform.position);
            w.Vector(blast.transform.localEulerAngles);
        }, size: 33);
    }

    /// <summary> Synchronizes the shockwave of the player. </summary>
    public static void SyncShock(GameObject shock, float force)
    {
        // checking if this is really a player's shockwave
        if (LobbyController.Lobby == null || shock?.name != "PhysicalShockwavePlayer(Clone)") return;
        Networking.Send(PacketType.Punch, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Byte(2);

            w.Float(force);
        }, size: 13);
    }

    #endregion
    #region damage

    /// <summary> Synchronizes damage dealt to the enemy. </summary>
    public static void SyncDamage(ulong enemyId, string hitter, float damage, bool explode, float critDamage)
    {
        byte type = (byte)System.Array.IndexOf(Types, hitter);
        if (type != 0xFF) Networking.Send(PacketType.DamageEntity, w =>
        {
            w.Id(enemyId);
            w.Enum(Networking.LocalPlayer.Team);
            w.Byte(type);

            w.Float(damage);
            w.Bool(explode);
            w.Float(critDamage);
        }, size: 19);
    }

    /// <summary> Deals bullet damage to the enemy. </summary>
    public static void DealDamage(EnemyIdentifier enemyId, Reader r)
    {
        r.Inc(1); // skip team because enemies don't have a team
        enemyId.hitter = Types[r.Byte()];
        enemyId.DeliverDamage(enemyId.gameObject, Vector3.zero, enemyId.transform.position, r.Float(), r.Bool(), r.Float(), NetDmg);
    }

    #endregion
}
