namespace Jaket.HarmonyPatches;

using HarmonyLib;
using Steamworks;
using ULTRAKILL.Cheats;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.UI;
using Jaket.World;

[HarmonyPatch(typeof(NewMovement), "Start")]
public class SpawnPatch
{
    static void Prefix(NewMovement __instance)
    {
        // add some randomness to the spawn position so players don't stack on top of each other at the start of the level
        if (LobbyController.Lobby != null) __instance.transform.position += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
    }
}

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
public class RespawnPatch
{
    static void Prefix()
    {
        // checkpoint destroys some objects on the level
        World.Instance.Invoke("Recache", .1f);

        // in the sandbox after death, enemies are not destroyed
        if (LobbyController.Lobby == null || !LobbyController.IsOwner || SceneHelper.CurrentScene == "uk_construct") return;

        // notify each client that the host has died so that they destroy all enemies
        LobbyController.EachMemberExceptOwner(member => Networking.SendEmpty(member.Id, PacketType.HostDied));
    }
}

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
public class DeathPatch
{
    static void Prefix(NewMovement __instance, int damage, bool invincible)
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

[HarmonyPatch(typeof(CheatsManager), nameof(CheatsManager.HandleCheatBind))]
public class CheatsPatch
{
    // cheats shouldn't work in chat or during animation
    static bool Prefix() => !Chat.Instance.Shown && Movement.Instance.Emoji == 0xFF;
}

[HarmonyPatch(typeof(CheatsController), nameof(CheatsController.Update))]
public class CheatsMenuPatch
{
    // cheat menu shouldn't appear in chat or during animation
    static bool Prefix() => !Chat.Instance.Shown && Movement.Instance.Emoji == 0xFF;
}

[HarmonyPatch(typeof(Noclip), nameof(Noclip.Update))]
public class NoclipPatch
{
    // this cheat shouldn't work in chat or during animation
    static bool Prefix() => !Chat.Instance.Shown && Movement.Instance.Emoji == 0xFF;
}
