namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.EntityTypes;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyIdentifierPatch
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

[HarmonyPatch(typeof(EnemyIdentifier), "Death")]
public class EnemyIdentifierPatchBoss
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this enemy was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        // only the host should report death
        if (!LobbyController.IsOwner || __instance.dead) return;

        // notify each client that the enemy has died
        byte[] enemyData = Writer.Write(w => w.Int(__instance.gameObject.GetComponent<LocalEnemy>().Id));
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, enemyData, PacketType.EnemyDied));

        // notify every client that the boss has died
        if (Networking.Bosses.Contains(__instance))
        {
            byte[] bossData = Writer.Write(w => w.String(__instance.gameObject.name));
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, bossData, PacketType.BossDefeated));
        }
    }
}
