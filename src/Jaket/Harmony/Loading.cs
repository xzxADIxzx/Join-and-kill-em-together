namespace Jaket.Harmony;

using HarmonyLib;

using Jaket.Assets;
using Jaket.Net;

public static class Loading
{
    [HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.LoadScene))]
    [HarmonyPostfix]
    static void Load() => Events.OnLoadingStart.Fire();

    [HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.RestartScene))]
    [HarmonyPostfix]
    static void Rest() => Events.OnLoadingStart.Fire();

    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
    [HarmonyPrefix]
    static bool After()
    {
        if (LobbyController.Offline || LobbyController.IsOwner) return true;

        Bundle.Hud("load-mission");
        return false;
    }

    [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel))]
    [HarmonyPrefix]
    static bool Other() => After();

    [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.GoToSavedLevel))]
    [HarmonyPrefix]
    static bool Saved() => After();
}
