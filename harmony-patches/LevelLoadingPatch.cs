namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
public class LevelLoadingPatch
{
    public static bool Prefix()
    {
        // if the player is the owner of the lobby, then everything is OK
        if (LobbyController.Lobby == null || LobbyController.IsOwner) return true;

        // otherwise, notify him that he need to wait for the host
        HudMessageReceiver.Instance.SendHudMessage("Wait for the lobby owner to load the level...");

        // prevent the ability to load before the host, because this leads to a bunch of bugs and fierce lags
        return false;
    }
}

[HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel))]
public class LevelChangerPatch
{
    static bool Prefix() => LevelLoadingPatch.Prefix();
}

[HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.GoToSavedLevel))]
public class SavedLevelPatch
{
    static bool Prefix() => LevelLoadingPatch.Prefix();
}

[HarmonyPatch(typeof(GameStateManager), "get_CanSubmitScores")]
public class SubmitScoresPatch
{
    static void Postfix(ref bool __result) => __result &= !Networking.WasMultiplayerUsed;
}

[HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetInfo))]
public class FinalRankPatch
{
    static void Postfix(FinalRank __instance)
    {
        if (Networking.WasMultiplayerUsed)
        {
            __instance.totalRank.transform.parent.GetComponent<Image>().color = Team.Pink.Data().Color();
            __instance.extraInfo.text += "- <color=#FF4BD3>MULTIPLAYER USED</color>\n";
        }
    }
}
