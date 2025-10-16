namespace Jaket.Harmony;

using Discord;
using HarmonyLib;
using Steamworks;

using Jaket.Net;

public static class RichPresence
{
    [HarmonyPatch(typeof(DiscordController), "SendActivity")]
    [HarmonyPrefix]
    static void Discord(ref Activity ___cachedActivity)
    {
        if (LobbyController.Offline) return;

        ___cachedActivity.State                  = "Multiplayer via Jaket";
        ___cachedActivity.Party.Size.CurrentSize = LobbyController.Lobby?.MemberCount ?? 0;
        ___cachedActivity.Party.Size.MaxSize     = LobbyController.Lobby?.MaxMembers  ?? 0;
    }

    [HarmonyPatch(typeof(DiscordController), "OnApplicationQuit")]
    [HarmonyPrefix]
    static bool Discord() => false; // I'd honestly been trying to figure it out, but at some point I just gave up

    [HarmonyPatch(typeof(SteamController), nameof(SteamController.FetchSceneActivity))]
    [HarmonyPrefix]
    static void Steam()
    {
        /* group friends according to the lobby they are joined to
         * to learn more about this, check out the following links
         * https://wiki.facepunch.com/steamworks/Grouping_Friends
         * https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence
         */
        if (LobbyController.Offline)
        {
            SteamFriends.SetRichPresence("steam_player_group", "");
            SteamFriends.SetRichPresence("steam_player_group_size", "");
        }
        else
        {
            SteamFriends.SetRichPresence("steam_player_group", LobbyConfig.Name);
            SteamFriends.SetRichPresence("steam_player_group_size", LobbyController.Lobby?.MemberCount.ToString());
        }
    }

    [HarmonyPatch(typeof(SteamFriends), nameof(SteamFriends.SetRichPresence))]
    [HarmonyPrefix]
    static void Steam(string key, ref string value)
    {
        if (LobbyController.Offline) return;

        // #AtCyberGrind is a localization string for Cyber Grind, just "%difficulty% | Cyber Grind: Wave %wave%" without setting its values
        if (key == "wave") value += " | Multiplayer via Jaket";

        // #AtStandardLevel is a localization string for other levels, just "%difficulty% | %level%" without setting its values
        if (key == "level") value += " | Multiplayer via Jaket";

        /* other steam_display values for ULTRAKILL include:
         * #AtMainMenu    (displays "Main Menu")
         * #AtCustomLevel (displays "Playing Custom Level")
         * #UnknownLevel  (displays "???")
         * these have no additional localization values, so we cannot add anything to them
         * for more info, check out SteamController.FetchSceneActivity(string scene)
         */
    }
}
