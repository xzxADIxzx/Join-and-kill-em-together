namespace Jaket.Patches;

using HarmonyLib;
using Steamworks;

using Jaket.Net;

[HarmonyPatch]
public class SteamPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamController), "FetchSceneActivity")]
    static void Activity()
    {
        // reset the friend group if the player is not in a lobby
        if (LobbyController.Offline)
        {
            SteamFriends.SetRichPresence("steam_player_group", "");
            SteamFriends.SetRichPresence("steam_player_group_size", "");
        }
        // or create a friend group and set the group size to the lobby member count
        // check out https://wiki.facepunch.com/steamworks/Grouping_Friends and https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence
        else
        {
            SteamFriends.SetRichPresence("steam_player_group", LobbyController.Lobby?.GetData("name"));
            SteamFriends.SetRichPresence("steam_player_group_size", LobbyController.Lobby?.MemberCount.ToString());
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamFriends), "SetRichPresence")]
    static void SetRichPresence(string key, ref string value)
    {
        if (LobbyController.Offline) return;

        // #AtCyberGrind is a localization string for Cyber Grind, just "%difficulty% | Cyber Grind: Wave %wave%" without setting its values
        if (key == "wave") value += " | Multiplayer via Jaket";

        // #AtStandardLevel is a localization string for other levels, just "%difficulty% | %level%" without setting its values
        if (key == "level") value += " | Multiplayer via Jaket";

        /* other steam_display values for ULTRAKILL include:
         * #AtMainMenu (displays "Main Menu" in your activity)
         * #AtCustomLevel (displays "Playing Custom Level" in your activity)
         * #UnknownLevel (displays "???" in your activity)
         * these have no additional localization values, so we cannot add "Multiplayer via Jaket" to them
         * for more info, check out SteamController.FetchSceneActivity(string scene)
         */
    }
}
