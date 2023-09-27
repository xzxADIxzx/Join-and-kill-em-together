namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Net;

[HarmonyPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
public class FinalRankPatch
{
    static bool Prefix()
    {
        // if the player is the owner of the lobby, then everything is OK
        if (LobbyController.Lobby == null || LobbyController.IsOwner) return true;

        // otherwise, notify him that he need to wait for the host
        HudMessageReceiver.Instance.SendHudMessage("Wait for the lobby owner to load the level...");

        // prevent the ability to load before the host, because this leads to a bunch of bugs and fierce lags
        return false;
    }
}
