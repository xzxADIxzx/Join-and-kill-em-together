namespace Jaket.HarmonyPatches;

using HarmonyLib;
using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyIdentifierPatch
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        if (LobbyController.IsOwner)
        {
            Networking.CurrentOwner = SteamClient.SteamId;
            var enemy = __instance.gameObject.AddComponent<RemoteEnemy>(); // TODO replace by LocalEnemy

            Networking.entities.Add(enemy);
            enemy.Type = (EntityType)Enemies.CopiedIndex(__instance.gameObject.name);
        }
        else Object.Destroy(__instance); // TODO ask host to spawn enemy if playing sandbox
    }
}
