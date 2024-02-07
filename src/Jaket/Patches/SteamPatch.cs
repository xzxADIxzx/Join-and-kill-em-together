namespace Jaket.Patches;

using HarmonyLib;

using Jaket.Net;
using Steamworks;
using Steamworks.Data;

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
        if (LobbyController.Lobby == null)
            return;
        /*
         * get your current steam_display value
         * the steam_display value is used for setting rich presence's localization string
         * it is pre-defined in the developer's steamworks settings (https://partner.steamgames.com/doc/api/ISteamFriends#richpresencelocalization)
        */
        var steamDisplay = SteamFriends.GetRichPresence("steam_display");

        // #AtCyberGrind is the steam_display localization string for Cyber Grind, just "%difficulty% | Cyber Grind: Wave %wave%" without setting its values
        if (key == "wave" && steamDisplay == "#AtCyberGrind")
            value += " | Multiplayer via Jaket";

        // #AtStandardLevel is the steam_display localization string for Cyber Grind, just "%difficulty% | %level% without setting its values
        if (key == "level" && steamDisplay == "#AtStandardLevel")
            value += " | Multiplayer via Jaket";
        /*
         * other steam_display values for ULTRAKILL include:
         * #AtMainMenu (displays "Main Menu" in your activity)
         * #AtCustomLevel (displays "Playing Custom Level" in your activity)
         * #UnknownLevel (displays "???" in your activity)
         * these have no additional localization values, this is why they are not included here
         * for more info, check out SteamController.FetchSceneActivity(string scene)
        */
    }

    static void RefreshGroupActivity(Lobby? lobby)
    {
        if (lobby == null)
        {
            //reset the friend group if not in a lobby
            SteamFriends.SetRichPresence("steam_player_group", "");
            SteamFriends.SetRichPresence("steam_player_group_size", "");
            return;
        }
        /*
         * create a friend group (lobby id as the value) and set the group size (lobby member count)
         * check out https://wiki.facepunch.com/steamworks/Grouping_Friends and https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence
        */
        SteamFriends.SetRichPresence("steam_player_group", lobby.Value.Id.ToString());
        SteamFriends.SetRichPresence("steam_player_group_size", lobby.Value.MemberCount.ToString());
    }
}

