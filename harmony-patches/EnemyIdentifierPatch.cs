namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.EntityTypes;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyPatch
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this enemy was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        bool boss = __instance.gameObject.GetComponent<BossHealthBar>() != null;
        if (boss) Networking.Bosses.Add(__instance);

        if (LobbyController.IsOwner)
            Networking.Entities.Add(__instance.gameObject.AddComponent<LocalEnemy>());
        else
        {
            if (boss)
                // will be used in the future to trigger the game's internal logic
                __instance.gameObject.SetActive(false);
            else
                // the enemy is no longer needed, so destroy it
                Object.Destroy(__instance.gameObject); // TODO ask host to spawn enemy if playing sandbox
        }
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
public class EnemyDamagePatch
{
    static bool Prefix(EnemyIdentifier __instance, Vector3 force, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon)
    {
        // if source weapon is null, then the damage was caused by the environment
        if (LobbyController.Lobby == null || sourceWeapon == null) return true;

        // network bullets are needed just for the visual, damage is done through packets
        if (sourceWeapon == Bullets.SynchronizedBullet) return false;

        // if the original weapon is network damage, then the damage was received over the network
        if (sourceWeapon == Bullets.NetworkDamage) return true;

        // if the enemy doesn't have an entity component, then it was created before the lobby
        if (!__instance.TryGetComponent<Entity>(out var entity)) return true;

        // if the damage was caused by the player himself, then all others must be notified about this
        byte[] data = Writer.Write(w =>
        {
            w.Int(entity.Id);
            w.Int((int)Networking.LocalPlayer.team);

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
        byte[] enemyData = Writer.Write(w => w.Int(enemy.Id));
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, enemyData, PacketType.EnemyDied));

        // notify every client that the boss has died
        if (Networking.Bosses.Contains(__instance))
        {
            byte[] bossData = Writer.Write(w => w.String(__instance.gameObject.name));
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, bossData, PacketType.BossDefeated));
        }
    }
}
