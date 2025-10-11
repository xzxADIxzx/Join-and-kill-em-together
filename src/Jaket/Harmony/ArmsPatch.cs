namespace Jaket.Harmony;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
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
            if ((caught = ___caughtEid).TryGetComponent(out Entity.Agent a) && a.Patron is Enemy e) e.TakeOwnage();
        }
        else caught = null;
    }

    static bool parried;

    [HarmonyPatch(typeof(Punch), "ActiveEnd")]
    [HarmonyPrefix]
    static void Punch()
    {
        if (LobbyController.Offline) return;

        Networking.Send(PacketType.Punch, 6, w =>
        {
            w.Id(AccId);
            w.Byte(0x00);

            w.Bool(parried);
            parried = false;
        });
    }

    [HarmonyPatch(typeof(Punch), nameof(global::Punch.GetParryLookTarget))]
    [HarmonyPrefix]
    static void Parry() => parried = true;
}
