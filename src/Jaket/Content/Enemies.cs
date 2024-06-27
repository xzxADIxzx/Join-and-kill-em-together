namespace Jaket.Content;

using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> List of all enemies in the game and some useful methods. </summary>
public class Enemies
{
    /// <summary> List of prefabs of all enemies. </summary>
    public static List<EnemyIdentifier> Prefabs = new();
    /// <summary> Dictionary of entity types to their classes. </summary>
    public static Dictionary<EntityType, Type> Types = new();

    /// <summary> Whether damage and death of enemies must be logged. </summary>
    public static bool Debug;

    /// <summary> Loads all enemies for future use. </summary>
    public static void Load()
    {
        Events.OnLoaded += () =>
        {
            int length = GameAssets.Enemies.Length; // custom enemy was added to the prefabs list
            if (Prefabs.Count != length) Prefabs.RemoveRange(length, Prefabs.Count - length);
        };

        foreach (var name in GameAssets.Enemies) Prefabs.Add(GameAssets.Enemy(name).GetComponentInChildren<EnemyIdentifier>());

        for (var type = EntityType.Filth; type <= EntityType.Puppet; type++) Types[type] = typeof(SimpleEnemy);
        Types[EntityType.Swordsmachine] = typeof(Swords);
        Types[EntityType.V2] = typeof(V2);
        Types[EntityType.V2_GreenArm] = typeof(V2);
        Types[EntityType.Sentry] = typeof(Turret);
        Types[EntityType.Gutterman] = typeof(Gutterman);
        Types[EntityType.MaliciousFace] = typeof(Body);
        Types[EntityType.HideousMass] = typeof(Shrimp);
        Types[EntityType.Idol] = typeof(Idol);
        Types[EntityType.Gabriel] = typeof(Gabriel);
        Types[EntityType.Gabriel_Angry] = typeof(Gabriel);
        Types[EntityType.Johninator] = typeof(V2);
    }

    /// <summary> Finds the entity type by enemy class and type, taking into account the fact that some enemies have the same types. </summary>
    public static EntityType Type(EnemyIdentifier id)
    {
        if (id == null) return EntityType.None;

        // there are the necessary crutches, because developers incorrectly set the types of some enemies
        switch (id.name.Contains("(") ? id.name.Substring(0, id.name.IndexOf("(")).Trim() : id.name)
        {
            case "V2 Green Arm Variant": return EntityType.V2_GreenArm;
            case "V2 Green Arm": return EntityType.V2_GreenArm;
            case "Very Cancerous Rodent": return EntityType.VeryCancerousRodent;
            case "DroneFlesh": return EntityType.FleshPrison_Eye;
            case "DroneSkull Variant": return EntityType.FleshPanopticon_Face;
            case "Mandalore": return EntityType.Mandalore;
            case "Big Johninator": return EntityType.Johninator;
            case "Big Johnator": return EntityType.Johninator;
        }

        // the remaining enemies can be found by their type
        int index = Prefabs.FindIndex(prefab => prefab.enemyClass == id.enemyClass && prefab.enemyType == id.enemyType);
        return index == -1 ? EntityType.None : (EntityType.EnemyOffset + index);
    }

    /// <summary> Spawns an enemy with the given type. </summary>
    public static Entity Instantiate(EntityType type)
    {
        // EnemyId of Malicious Face and Cerberus is in a child object
        // https://discord.com/channels/1132614140414935070/1132614140876292190/1146507403102257162
        var obj = type != EntityType.MaliciousFace && type != EntityType.Cerberus ?
                Entities.Mark(Prefabs[type - EntityType.EnemyOffset].gameObject) :
                Entities.Mark(Prefabs[type - EntityType.EnemyOffset].transform.parent.gameObject).transform.GetChild(0).gameObject;

        // repeat this, since only the parental object was renamed
        obj.name = "Net";

        return obj.AddComponent(Types[type]) as Entity;
    }

    /// <summary> Synchronizes the enemy between network members. </summary>
    public static bool Sync(EnemyIdentifier enemyId)
    {
        if (LobbyController.Offline || enemyId.dead || enemyId.name == "Net") return true;

        // levels 2-4, 5-4, 7-1 and 7-4 contain unique bosses that needs to be dealt with separately
        if (Tools.Scene == "Level 2-4" && enemyId.name == "MinosArm")
        {
            enemyId.gameObject.AddComponent<Hand>();
            return true;
        }
        // there is no need to sync the fake, since the coins are synced
        if (Tools.Scene == "Level 5-2" && enemyId.name == "FerrymanIntro") return true;
        if (Tools.Scene == "Level 5-4" && enemyId.name == "Leviathan")
        {
            enemyId.gameObject.AddComponent<Leviathan>();
            return true;
        }
        if (Tools.Scene == "Level 7-1" && enemyId.name == "MinotaurChase")
        {
            enemyId.gameObject.AddComponent<Minotaur>();
            return true;
        }
        // the security system is a complex enemy consisting of several subenemies
        if (Tools.Scene == "Level 7-4" && enemyId.GetComponentInParent<CombinedBossBar>() != null) return true;
        if (Tools.Scene == "Level 7-4" && enemyId.name == "KillAllEnemiesChecker") return true; // what is that?!
        if (Tools.Scene == "Level 7-4" && enemyId.name == "Brain")
        {
            enemyId.gameObject.AddComponent<Brain>();
            return true;
        }

        if (LobbyController.IsOwner || enemyId.TryGetComponent<Sandbox.SandboxEnemy>(out _))
        {
            enemyId.gameObject.AddComponent(Types[Type(enemyId)]);
            return true;
        }
        else
        {
            Tools.DestroyImmediate(enemyId.name != "Body" && enemyId.name != "StatueBoss" ? enemyId.gameObject : enemyId.transform.parent.gameObject);
            return false;
        }
    }

    /// <summary> Synchronizes the damage dealt to the enemy. </summary>
    public static bool SyncDamage(EnemyIdentifier enemyId, float damage, float critDamage, GameObject source)
    {
        if (LobbyController.Offline || enemyId.dead) return true;
        if (Debug) Log.Debug($"{(source == Bullets.NetDmg ? "Network" : source == Bullets.Fake ? "Fake" : "Local")} damage was dealt: {damage}, {critDamage}, {source?.name}");

        if (source == Bullets.NetDmg) return true; // the damage was received over the network
        if (source == Bullets.Fake) return false; // bullets are only needed for visual purposes and mustn't cause damage

        if (enemyId.TryGetComponent<Entity>(out var entity) && (entity is not RemotePlayer player || !player.Doll.Dashing))
            Bullets.SyncDamage(entity.Id, enemyId.hitter, damage, critDamage);

        return true;
    }

    /// <summary> Synchronizes the death of the enemy. </summary>
    public static void SyncDeath(EnemyIdentifier enemyId)
    {
        if (LobbyController.Offline || enemyId.dead) return;
        if (enemyId.TryGetComponent<Enemy>(out var enemy) && !enemy.Dead)
        {
            if (Debug) Log.Debug($"Enemy#{enemy.Id} died :(");
            enemy.NetKill();
        }
    }

    /// <summary> Finds the most suitable target for the enemy, that is the closest player. </summary>
    public static void FindTarget(EnemyIdentifier enemyId) => Stats.MeasureTime(ref Stats.TargetUpdate, () =>
    {
        if (LobbyController.Offline || enemyId.dead) return;

        // update target only if the current target is the local player
        if (enemyId.target == null || !enemyId.target.isPlayer) return;

        var enemy = enemyId.transform.position;
        var target = NewMovement.Instance.transform;
        var dst = (enemy - target.position).sqrMagnitude;

        Networking.EachPlayer(player =>
        {
            var newDst = (enemy - player.transform.position).sqrMagnitude;
            if (newDst < dst && player.Health > 0)
            {
                target = player.transform;
                dst = newDst;
            }
        });

        // update the target if there is a remote player that is closer to the enemy than you
        if (target != NewMovement.Instance.transform) enemyId.target = new(target);
    });
}
