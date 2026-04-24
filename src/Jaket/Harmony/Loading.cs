namespace Jaket.Harmony;

using Jaket.Assets;
using Jaket.Net;

public static class Loading
{
    [StaticPatch(typeof(SceneHelper), nameof(SceneHelper.LoadSceneAsync))]
    [StaticPatch(typeof(SceneHelper), nameof(SceneHelper.RestartSceneAsync))]
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
}
