namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.EntityTypes;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyStartPatch
{
    static bool Prefix(EnemyIdentifier __instance)
    {
        // the player is not connected, nothing needs to be done
        if (LobbyController.Lobby == null) return true;

        // I don't even want to know why
        if (__instance.dead) return true;

        // level 0-2 contains several cutscenes that do not need to be removed
        if (SceneHelper.CurrentScene == "Level 0-2" && __instance.enemyType == EnemyType.Swordsmachine && __instance.GetComponent<BossHealthBar>() == null) return true;

        // level 5-4 contains a unique boss that needs to be dealt with separately. 
        if (SceneHelper.CurrentScene == "Level 5-4" && __instance.enemyType == EnemyType.Leviathan)
        {
            var leviathan = __instance.gameObject.AddComponent<Leviathan>();
            if (LobbyController.IsOwner) Networking.Entities[leviathan.Id] = leviathan;

            return true;
        }

        // the enemy was created remotely
        if (__instance.gameObject.name == "Net")
        {
            // add spawn effect for better look
            __instance.spawnIn = true;

            // teleport the enemy so that the spawn effect does not appear at the origin
            if (__instance.TryGetComponent<RemoteEnemy>(out var enemy)) __instance.transform.position = new(enemy.x.target, enemy.y.target, enemy.z.target);

            return true;
        }

        bool boss = __instance.gameObject.GetComponent<BossHealthBar>() != null;
        if (boss) Networking.Bosses.Add(__instance);

        if (LobbyController.IsOwner)
        {
            var enemy = __instance.gameObject.AddComponent<LocalEnemy>();
            Networking.Entities[enemy.Id] = enemy;

            return true;
        }
        else
        {
            if (boss)
                // will be used in the future to trigger the game's internal logic
                __instance.gameObject.SetActive(false);
            else
                // the enemy is no longer needed, so destroy it
                Object.Destroy(__instance.gameObject); // TODO ask host to spawn enemy if playing sandbox

            return false;
        }
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
public class EnemyDamagePatch
{
    static bool Prefix(EnemyIdentifier __instance, Vector3 force, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon)
    {
        // whether the damage was dealt with a melee weapon
        bool melee = Bullets.Melee.Contains(__instance.hitter);

        // if source weapon is null, then the damage was caused by the environment
        if (LobbyController.Lobby == null || (sourceWeapon == null && !melee)) return true;

        // network bullets are needed just for the visual, damage is done through packets
        if (sourceWeapon == Bullets.SynchronizedBullet) return false;

        // if the original weapon is network damage, then the damage was received over the network
        if (sourceWeapon == Bullets.NetworkDamage) return true;

        // if the enemy doesn't have an entity component, then it was created before the lobby
        if (!__instance.TryGetComponent<Entity>(out var entity)) return true;

        // if the damage was caused by the player himself, then all others must be notified about this
        byte[] data = Writer.Write(w =>
        {
            w.Id(entity.Id);
            w.Byte((byte)Networking.LocalPlayer.team);
            w.Bool(melee);

            w.Vector(force);
            w.Float(multiplier);
            w.Bool(tryForExplode);
            w.Float(critMultiplier);
        });

        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.DamageEntity));
        else
            Networking.Send(LobbyController.Owner, data, PacketType.DamageEntity);

        return true;
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death))]
public class EnemyDeathPatch
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this enemy was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        // only the host should report death
        if (!LobbyController.IsOwner || __instance.dead) return;

        // if the enemy doesn't have an entity component, then it was created before the lobby
        if (!__instance.TryGetComponent<LocalEnemy>(out var enemy)) return;

        // notify each client that the enemy has died
        byte[] enemyData = Writer.Write(w => w.Id(enemy.Id));
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, enemyData, PacketType.EnemyDied));

        // notify every client that the boss has died
        if (Networking.Bosses.Contains(__instance))
        {
            byte[] bossData = Writer.Write(w => w.String(__instance.gameObject.name));
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, bossData, PacketType.BossDefeated));
        }

        // destroy the component so that it is no longer synchronized over the network
        Object.Destroy(enemy);
    }
}
