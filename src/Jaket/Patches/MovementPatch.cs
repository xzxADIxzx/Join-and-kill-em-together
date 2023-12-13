namespace Jaket.HarmonyPatches;

using HarmonyLib;
using Steamworks;
using ULTRAKILL.Cheats;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.World;

[HarmonyPatch(typeof(NewMovement))]
public class MovementPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static void Spawn(NewMovement __instance)
    {
        // add some randomness to the spawn position so players don't stack on top of each other at the start of the level
        if (LobbyController.Lobby != null) __instance.transform.position += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NewMovement.GetHurt))]
    static void Death(NewMovement __instance, int damage, bool invincible)
    {
        // sometimes fake death messages are sent to the chat
        if (invincible && __instance.gameObject.layer == 15) return;

        if (__instance.hp > 0 && __instance.hp - damage <= 0)
        {
            // player death message
            LobbyController.Lobby?.SendChatString($"<system><color=orange>Player {SteamClient.Name} died.</color>");

            // close the chat to prevent some bugs
            Chat.Instance.field.gameObject.SetActive(false);

            // interrupt the emoji to avoid bugs
            Movement.Instance.StartEmoji(0xFF);
        }
    }
}

[HarmonyPatch]
public class CommonPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CheatsManager), nameof(CheatsManager.HandleCheatBind))]
    static bool Cheats() => !UI.AnyMovementBlocking() && Movement.Instance.Emoji == 0xFF;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CheatsController), nameof(CheatsController.Update))]
    static bool CheatsMenu() => Cheats();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Noclip), nameof(Noclip.Update))]
    static bool CheatsNoclip() => Cheats();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Grenade), "Update")]
    static bool RocketRide() => Cheats();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CameraFrustumTargeter), "CurrentTarget", MethodType.Setter)]
    static void AutoAim(ref Collider value)
    {
        if (value != null && value.TryGetComponent<RemotePlayer>(out var player) && player.team.Ally()) value = null;
    }
}
