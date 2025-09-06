namespace Jaket.Harmony;

using HarmonyLib;
using UnityEngine;

using Jaket.Net;
using Jaket.Net.Types;

public static class ArmsPatch
{
    static EnemyIdentifier caught;

    [HarmonyPatch(typeof(HookArm), "FixedUpdate")]
    [HarmonyPostfix]
    static void Hook(bool ___forcingFistControl, Vector3 ___hookPoint, HookState ___state, EnemyIdentifier ___caughtEid, bool ___lightTarget)
    {
        if (LobbyController.Offline) return;

        Networking.LocalPlayer.Hook = ___forcingFistControl ? ___hookPoint : Vector3.zero;

        if (___state == HookState.Pulling && ___caughtEid && ___lightTarget)
        {
            if (caught == ___caughtEid) return;
            if ((caught = ___caughtEid).TryGetComponent(out Entity.Agent a) && a.Patron is OwnableEntity e) e.TakeOwnage();
        }
        else caught = null;
    }
}
