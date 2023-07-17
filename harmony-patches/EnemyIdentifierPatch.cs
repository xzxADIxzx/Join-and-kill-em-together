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
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
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
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net" || __instance.dead) return;

        // send boss death notice
        if (LobbyController.IsOwner && Networking.Bosses.Contains(__instance))
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, Writer.Write(w => w.String(__instance.gameObject.name)), PacketType.BossDefeated));
    }
}
