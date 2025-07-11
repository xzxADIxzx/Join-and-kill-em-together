/*
namespace Jaket.ObsoletePatches;

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch]
public class ArmsPatch
{
    static LocalPlayer lp => Networking.LocalPlayer;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Punch), "ActiveStart")]
    static void Puncn()
    {
        if (LobbyController.Offline) return;

        foreach (var harpoon in NewMovement.Instance.GetComponentsInChildren<Harpoon>()) Bullets.Punch(harpoon, true);
        Bullets.SyncPunch();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Punch), nameof(Punch.GetParryLookTarget))]
    static void Parry() => lp.Parried = true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HookArm), "Update")]
    static void Hook(HookArm __instance, bool ___forcingFistControl, Vector3 ___hookPoint, bool ___lightTarget, EnemyIdentifier ___caughtEid)
    {
        if (LobbyController.Offline) return;

        lp.Hook = ___forcingFistControl ? ___hookPoint : Vector3.zero;
        if (__instance.state == HookState.Pulling && ___lightTarget && ___caughtEid.name == "Net") ___caughtEid.GetComponent<Enemy>()?.TakeOwnage();
    }
}
*/
