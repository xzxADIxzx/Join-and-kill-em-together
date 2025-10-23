namespace Jaket.ObsoletePatches;

using HarmonyLib;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;

using static Jaket.UI.Lib.Pal;

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
        if (Networking.WasMultiplayerUsed) __instance.extraInfo.text += "- <color=#FF66CC>MULTIPLAYER USED</color>\n";
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetRank))]
    static void RankColor(FinalRank __instance)
    {
        if (Networking.WasMultiplayerUsed)
        {
            __instance.totalRank.transform.parent.GetComponent<Image>().color = pink;
            __instance.totalRank.text = Bundle.CutColors(__instance.totalRank.text);
        }
    }
}
