/*
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
    /// <summary> Spawns an enemy with the given type. </summary>
    public static Entity Instantiate(EntityType type)
    {
        // EnemyId of Malicious Face and Cerberus is in a child object
        // https://discord.com/channels/1132614140414935070/1132614140876292190/1146507403102257162
        var obj = type != EntityType.MaliciousFace && type != EntityType.Cerberus
            ? Entities.Mark(Prefabs[type - EntityType.Filth].gameObject)
            : Entities.Mark(Prefabs[type - EntityType.Filth].transform.parent.gameObject).transform.GetChild(0).gameObject;

        // repeat this, since only the parental object was renamed
        obj.name = "Net";

        return obj.AddComponent(Types[type]) as Entity;
    }

    /// <summary> Synchronizes the enemy between network members. </summary>
    public static bool Sync(EnemyIdentifier enemyId)
    {
        if (LobbyController.Offline || enemyId.dead) return true;
        if (Scene == "Endless") enemyId.spawnEffect = null;
        if (enemyId.name == "Net") return true;

        // levels 2-4, 5-4, 7-1 and 7-4 contain unique bosses that needs to be dealt with separately
        if (Scene == "Level 2-4" && enemyId.name == "MinosArm")
        {
            enemyId.gameObject.AddComponent<Hand>();
            return true;
        }
        // there is no need to sync the fake, since the coins are synced
        if (Scene == "Level 5-2" && enemyId.name == "FerrymanIntro") return true;
        if (Scene == "Level 5-4" && enemyId.name == "Leviathan")
        {
            enemyId.gameObject.AddComponent<Leviathan>();
            return true;
        }
        if (Scene == "Level 7-1" && enemyId.name == "MinotaurChase")
        {
            enemyId.gameObject.AddComponent<Minotaur>();
            return true;
        }
        // the security system is a complex enemy consisting of several subenemies
        if (Scene == "Level 7-4" && enemyId.GetComponentInParent<CombinedBossBar>() != null) return true;
        if (Scene == "Level 7-4" && enemyId.name == "KillAllEnemiesChecker") return true; // what is that?!
        if (Scene == "Level 7-4" && enemyId.name == "Brain")
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
            Imdt(enemyId.name != "Body" && enemyId.name != "StatueBoss" ? enemyId.gameObject : enemyId.transform.parent.gameObject);
            return false;
        }
    }

    /// <summary> Finds the most suitable target for the enemy, that is the closest player. </summary>
    public static void FindTarget(EnemyIdentifier enemyId) => Stats.MeasureTime(ref Stats.TargetMs, () =>
    {
        if (LobbyController.Offline || enemyId.dead) return;

        // update target only if the current target is the local player
        if (enemyId.target == null || !enemyId.target.isPlayer) return;

        var enemy = enemyId.transform.position;
        var target = NewMovement.Instance.transform;
        var dst = (enemy - target.position).sqrMagnitude;

        Networking.Entities.Player(player =>
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
*/
