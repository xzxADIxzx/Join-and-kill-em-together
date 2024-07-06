namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(EnemyIdentifier))]
public class EnemyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static bool Spawn(EnemyIdentifier __instance) => Enemies.Sync(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
    static bool Damage(EnemyIdentifier __instance, float multiplier, float critMultiplier, GameObject sourceWeapon) => Enemies.SyncDamage(__instance, multiplier, critMultiplier, sourceWeapon);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.Death), typeof(bool))]
    static void Death(EnemyIdentifier __instance) => Enemies.SyncDeath(__instance);

    [HarmonyPrefix]
    [HarmonyPatch("UpdateTarget")]
    static bool Skip() => Time.frameCount % (1 + Networking.Entities.Count / 16) == 0;

    [HarmonyPostfix]
    [HarmonyPatch("UpdateTarget")]
    static void Target(EnemyIdentifier __instance) => Enemies.FindTarget(__instance);
}

[HarmonyPatch]
public class LogicPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SwordsMachine), "Start")]
    static void OutroSM(ref bool ___bossVersion)
    {
        if (LobbyController.Online && (Tools.Scene == "Level 0-2" || Tools.Scene == "Level 0-3" || Tools.Scene == "Level 1-3")) ___bossVersion = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void IntroV2(V2 __instance)
    {
        if (LobbyController.Online && Tools.Scene == "Level 1-4") __instance.intro = __instance.longIntro = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void OutroV2(ref bool ___bossVersion)
    {
        if (LobbyController.Online && (Tools.Scene == "Level 1-4" || Tools.Scene == "Level 4-4")) ___bossVersion = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpiderBody), "BreakCorpse")]
    static void BreakLogic(SpiderBody __instance)
    {
        if (LobbyController.Online && __instance.TryGetComponent<Entity>(out var body)) body.NetKill();
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
        if (LobbyController.Online && Tools.Scene == "Level 3-2") ___bossVersion = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GabrielSecond), "Start")]
    static void OutroG2(ref bool ___bossVersion)
    {
        if (LobbyController.Online && Tools.Scene == "Level 6-2") ___bossVersion = true;
    }
}

[HarmonyPatch]
public class OtherPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.AddPoints))]
    static void StyleHudErrorFix(ref GameObject sourceWeapon) => sourceWeapon = sourceWeapon == Bullets.NetDmg ? null : sourceWeapon;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EventOnDestroy), "OnDestroy")]
    static bool Destroy() => LobbyController.Offline || LobbyController.IsOwner;
}
