namespace Jaket.Harmony;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;

public static class Loading
{
    [StaticPatch(typeof(SceneHelper), nameof(SceneHelper.LoadSceneAsync))]
    [Postfix]
    static void Load() => Events.OnLoadingStart.Fire();

    [DynamicPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
    [Prefix]
    static bool After()
    {
        if (LobbyController.IsOwner) return true;

        Bundle.Hud("load-mission");
        return false;
    }

    [DynamicPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel))]
    [Prefix]
    static bool Other() => After();

    [DynamicPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.GoToSavedLevel))]
    [Prefix]
    static bool Saved() => After();

    [DynamicPatch(typeof(LeaderboardController), nameof(LeaderboardController.LeaderboardsBlocked), HarmonyLib.MethodType.Getter)]
    [Postfix]
    static void Score(ref bool __result) => __result = true;

    [DynamicPatch(typeof(GameProgressSaver), nameof(GameProgressSaver.SaveRank))]
    [Prefix]
    static bool Save() { Progress.Save(); return false; }

    [StaticPatch(typeof(GameProgressSaver), nameof(GameProgressSaver.WipeSlot))]
    [Prefix]
    static void Wipe(int slot) => Files.IterAll(Files.Join(GameProgressSaver.BaseSavePath, $"Slot{slot + 1}"), Files.Delete, "*.jaket");
}
