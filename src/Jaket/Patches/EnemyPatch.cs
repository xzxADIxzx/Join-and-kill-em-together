namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch(typeof(EnemyIdentifier))]
public class EnemyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static bool Start(EnemyIdentifier __instance) => Enemies.Sync(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
    static bool Damage(EnemyIdentifier __instance, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon) =>
        Enemies.SyncDamage(__instance, multiplier, tryForExplode, critMultiplier, sourceWeapon);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.Death))]
    static void Death(EnemyIdentifier __instance) => Enemies.SyncDeath(__instance);
}

[HarmonyPatch]
public class OtherPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void Intro(V2 __instance)
    {
        if (LobbyController.Lobby != null && SceneHelper.CurrentScene == "Level 1-4" && !__instance.secondEncounter)
            __instance.intro = __instance.longIntro = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(V2), "Start")]
    static void Outro(V2 __instance, ref bool ___bossVersion) => ___bossVersion = __instance.intro;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Idol), "SlowUpdate")]
    static bool IdolsLogic() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FerrymanFake), "Update")]
    static bool FerryLogic() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FerrymanFake), nameof(FerrymanFake.OnLand))]
    static void FerryDeath(FerrymanFake __instance)
    {
        if (LobbyController.IsOwner && __instance.TryGetComponent<Enemy>(out var enemy)) Networking.Send(PacketType.KillEntity, w => w.Id(enemy.Id), size: 8);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BossHealthBar), "Awake")]
    static void BossBar(BossHealthBar __instance)
    {
        if (LobbyController.Lobby == null || !__instance.TryGetComponent<EnemyIdentifier>(out var enemyId)) return;

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
