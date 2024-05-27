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

    [HarmonyPostfix]
    [HarmonyPatch("UpdateTarget")]
    static void Target(EnemyIdentifier __instance) => Enemies.FindTarget(__instance);
}

[HarmonyPatch]
public class LogicPatch
{
    static bool Update(Object obj) => LobbyController.Offline || obj.name == "Local";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void Intro(V2 __instance)
    {
        if (LobbyController.Online && Tools.Scene == "Level 1-4") __instance.intro = __instance.longIntro = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void Outro(V2 __instance, ref bool ___bossVersion) => ___bossVersion = __instance.intro;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpiderBody), "BreakCorpse")]
    static void BreakLogic(SpiderBody __instance)
    {
        if (LobbyController.Online && __instance.TryGetComponent<Entity>(out var body)) body.NetKill();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Idol), "SlowUpdate")]
    static bool IdolsLogic(Idol __instance) => Update(__instance);
}

[HarmonyPatch]
public class OtherPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EventOnDestroy), "OnDestroy")]
    static bool Destroy() => LobbyController.Offline || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BossHealthBar), "Awake")]
    static void BossBar(BossHealthBar __instance)
    {
        if (LobbyController.Offline || !__instance.TryGetComponent<EnemyIdentifier>(out var enemyId)) return;

        if (LobbyController.IsOwner || enemyId.enemyType == EnemyType.Minos || enemyId.enemyType == EnemyType.Leviathan)
        {
            // get the PPP value if the player is not the host
            if (!LobbyController.IsOwner) float.TryParse(LobbyController.Lobby?.GetData("ppp"), out LobbyController.PPP);

            enemyId.ForceGetHealth(); // the health of the identifier changes, it's only an indicator of real health, so you can do whatever you want with it
            LobbyController.ScaleHealth(ref enemyId.health);

            // boss bar will do all the work
            if (__instance.healthLayers == null) return;

            if (__instance.healthLayers.Length == 0)
                __instance.healthLayers = new HealthLayer[] { new() { health = enemyId.health } };
            else
            {
                float sum = 0f; // sum of the health of all layers except the last one
                for (int i = 0; i < __instance.healthLayers.Length - 1; i++) sum += __instance.healthLayers[i].health;

                // change the health of the last bar so that it does not turn green
                __instance.healthLayers[__instance.healthLayers.Length - 1].health = enemyId.health - sum;
            }
        }
        else if (__instance.TryGetComponent<Enemy>(out var enemy)) __instance.healthLayers = enemy.Layers();
    }
}
