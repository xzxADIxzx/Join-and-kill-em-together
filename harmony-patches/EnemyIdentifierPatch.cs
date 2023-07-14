namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Net;

[HarmonyPatch(typeof(EnemyIdentifier), "Start")]
public class EnemyIdentifierPatch
{
    static void Prefix(EnemyIdentifier __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        if (LobbyController.IsOwner)
            Networking.entities.Add(__instance.gameObject.AddComponent<LocalEnemy>());
        else
            Object.Destroy(__instance); // TODO ask host to spawn enemy if playing sandbox
    }
}
