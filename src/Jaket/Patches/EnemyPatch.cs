/*
[HarmonyPatch]
public class LogicPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void IntroV2(V2 __instance)
    {
        if (LobbyController.Online && Scene == "Level 1-4") __instance.intro = __instance.longIntro = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void OutroV2(ref bool ___bossVersion)
    {
        if (LobbyController.Online && (Scene == "Level 1-4" || Scene == "Level 4-4")) ___bossVersion = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Gutterman), "Explode")]
    static void BreakLogic(Gutterman __instance)
    {
        if (LobbyController.Online && __instance.TryGetComponent<Entity>(out var gman)) gman.NetKill();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Idol), "SlowUpdate")]
    static bool IdolsLogic(Idol __instance) => LobbyController.Offline || __instance.name == "Local";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gabriel), "Start")]
    static void OutroG1(ref bool ___bossVersion)
    {
        if (LobbyController.Online && Scene == "Level 3-2") ___bossVersion = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GabrielSecond), "Start")]
    static void OutroG2(ref bool ___bossVersion)
    {
        if (LobbyController.Online && Scene == "Level 6-2") ___bossVersion = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Wicked), "Update")]
    static void WickedLogic(EnemyIdentifier ___eid)
    {
        if (LobbyController.Online) ___eid.totalSpeedModifier = 1f + LobbyConfig.PPP;
    }
}
*/
