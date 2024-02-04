namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(EndlessGrid))]
public class CyberGrindPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static bool Enter() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch("LoadPattern")]
    static void Load(ref ArenaPattern pattern)
    {
        if (LobbyController.Lobby == null) return;
        if (LobbyController.IsOwner)
            CyberGrind.SyncPattern(pattern);
        else
            pattern = CyberGrind.CurrentPattern;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void Update(EndlessGrid __instance, ref ActivateNextWave ___anw)
    {
        if (LobbyController.Lobby != null && !LobbyController.IsOwner)
        {
            // set the current wave number to the synced one
            __instance.currentWave = CyberGrind.CurrentWave;
            // prevent the launch of a new wave on the client in order to have time to synchronize it
            ___anw.deadEnemies = -999;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Counter(ref Text ___enemiesLeftText)
    {
        if (LobbyController.Lobby != null && !LobbyController.IsOwner)
        {
            var list = EnemyTracker.Instance.enemies;
            list.RemoveAll(e => e == null || e.dead);

            // the original counter is broken, so you have to do everything yourself
            ___enemiesLeftText.text = list.Count.ToString();
        }
    }
}
