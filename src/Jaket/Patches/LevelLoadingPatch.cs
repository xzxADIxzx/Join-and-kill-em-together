namespace Jaket.Patches;

using HarmonyLib;
using System.Text.RegularExpressions;
using UnityEngine.UI;

using Jaket.Net;

using static Jaket.UI.Pal;

[HarmonyPatch]
public class LevelLoadingPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
    static bool AfterLevel()
    {
        // if the player is the owner of the lobby, then everything is OK
        if (LobbyController.Offline || LobbyController.IsOwner) return true;

        // otherwise, notify him that he need to wait for the host and prevent the next level from loading
        HudMessageReceiver.Instance.SendHudMessage("Wait for the lobby owner to load the level...");
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.GoToSavedLevel))]
    static bool SavedLevel() => AfterLevel();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel))]
    static bool LevelChanger() => AfterLevel();
}

[HarmonyPatch]
public class RankPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameStateManager), "CanSubmitScores", MethodType.Getter)]
    static void ScoresSubmission(ref bool __result) => __result &= !Networking.WasMultiplayerUsed;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetInfo))]
    static void RankExtra(FinalRank __instance)
    {
        if (Networking.WasMultiplayerUsed)
        {
            __instance.totalRank.transform.parent.GetComponent<Image>().color = pink;
            __instance.extraInfo.text += "- <color=#FF66CC>MULTIPLAYER USED</color>\n";
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetRank))]
    static void RankColor(FinalRank __instance)
    {
        if (Networking.WasMultiplayerUsed) __instance.totalRank.text = Regex.Replace(__instance.totalRank.text, "<.*?>", "");
    }
}
