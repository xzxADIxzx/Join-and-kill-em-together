namespace Jaket.HarmonyPatches;

using Discord;
using HarmonyLib;

using Jaket.Net;

[HarmonyPatch(typeof(DiscordController), "SendActivity")]
public class DiscordControllerPatch
{
    static void Prefix(ref Activity ___cachedActivity)
    {
        if (LobbyController.Lobby != null) ___cachedActivity.State = "Playing multiplayer via Jaket";
    }
}
