namespace Jaket.Content;

using System;
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
        foreach (var weapon in Weapons.Prefabs)
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

        Fake = UI.Object("Fake", Networking.Instance.transform);
        NetDmg = UI.Object("Network Damage", Networking.Instance.transform);
    }

    #region special

    /// <summary> Sends the explosion of the knuckleblaster. </summary>
    public static void SendBlast(GameObject blast)
    {
        // checking if this is really knuckleblaster explosion
        if (LobbyController.Lobby == null || blast?.name != "Explosion Wave(Clone)") return;
        Networking.Send(PacketType.Punch, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Byte(1);

            w.Vector(blast.transform.position);
            w.Vector(blast.transform.localEulerAngles);
        }, size: 29);
    }

    /// <summary> Sends the shockwave of the player. </summary>
    public static void SendShock(GameObject shock, float force)
    {
        // checking if this is really a player's shockwave
        if (LobbyController.Lobby == null || shock?.name != "PhysicalShockwavePlayer(Clone)") return;
        Networking.Send(PacketType.Punch, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Byte(2);

            w.Float(force);
        }, size: 9);
    }

    #endregion
    #region damage

    /// <summary> Synchronizes damage dealt to the enemy. </summary>
    public static void SyncDamage(ulong enemyId, string hitter, float damage, bool explode, float critDamage)
    {
        byte type = (byte)Array.IndexOf(Types, hitter);
        if (type != 0xFF) Networking.Send(PacketType.DamageEntity, w =>
        {
            w.Id(enemyId);
            w.Enum(Networking.LocalPlayer.Team);
            w.Byte(type);

            w.Float(damage);
            w.Bool(explode);
            w.Float(critDamage);
        }, size: 15);
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
