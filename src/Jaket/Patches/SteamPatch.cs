namespace Jaket.Patches;

using HarmonyLib;
using Steamworks;
using Steamworks.Data;

using Jaket.Net;

[HarmonyPatch]
public class SteamPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamController), "FetchSceneActivity")]
    static void Activity() => RefreshGroupActivity(LobbyController.Lobby);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamFriends), "SetRichPresence")]
    static void SetRichPresence(string key, ref string value)
    {
        if (LobbyController.Lobby == null) return;

        // the steam_display value is used to configure the rich presence's localization string
        // it is predefined in the developer's Steamworks settings (https://partner.steamgames.com/doc/api/ISteamFriends#richpresencelocalization)
        var steamDisplay = SteamFriends.GetRichPresence("steam_display");

        // #AtCyberGrind is the steam_display localization string for Cyber Grind, just "%difficulty% | Cyber Grind: Wave %wave%" without setting its values
        if (key == "wave" && steamDisplay == "#AtCyberGrind")
            value += " | Multiplayer via Jaket";

        // #AtStandardLevel is the steam_display localization string for all other levels, just "%difficulty% | %level% without setting its values
        if (key == "level" && steamDisplay == "#AtStandardLevel")
            value += " | Multiplayer via Jaket";

        /* other steam_display values for ULTRAKILL include:
         * #AtMainMenu (displays "Main Menu" in your activity)
         * #AtCustomLevel (displays "Playing Custom Level" in your activity)
         * #UnknownLevel (displays "???" in your activity)
         * these have no additional localization values, so we cannot add "| Multiplayer via Jaket" to them
         * for more info, check out SteamController.FetchSceneActivity(string scene)
         */
    }

    static void RefreshGroupActivity(Lobby? lobby)
    {
        // reset the friend group if the player is not in a lobby
        if (lobby == null)
        {
            SteamFriends.SetRichPresence("steam_player_group", "");
            SteamFriends.SetRichPresence("steam_player_group_size", "");
        }
        // or create a friend group and set the group size to the lobby member count
        // check out https://wiki.facepunch.com/steamworks/Grouping_Friends and https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence
        else
        {
            SteamFriends.SetRichPresence("steam_player_group", lobby.Value.GetData("name"));
            SteamFriends.SetRichPresence("steam_player_group_size", lobby.Value.MemberCount.ToString());
        }
    }
}

