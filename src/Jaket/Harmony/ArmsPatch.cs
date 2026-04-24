namespace Jaket.Harmony;

using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

public static class ArmsPatch
{
    static EnemyIdentifier caught;

    [DynamicPatch(typeof(HookArm), nameof(HookArm.FixedUpdate))]
    [Postfix]
    static void Hook(bool ___forcingFistControl, Vector3 ___hookPoint, HookState ___state, EnemyIdentifier ___caughtEid, bool ___lightTarget)
    {
        Networking.LocalPlayer.Hook = ___forcingFistControl ? ___hookPoint : Vector3.zero;

        if (___state == HookState.Pulling && ___caughtEid && ___lightTarget)
        {
            if (caught == ___caughtEid) return;
            if ((caught = ___caughtEid).TryGetEntity(out Enemy e)) e.TakeOwnage();
        }
        else caught = null;
    }

    static bool parried;

    [DynamicPatch(typeof(Punch), nameof(global::Punch.ActiveEnd))]
    [Postfix]
    static void Punch() => Networking.Send(PacketType.Punch, 6, w =>
    {
        w.Id(AccId);
        w.Byte(0x00);

        w.Bool(parried);
        parried = false;
    });

    [DynamicPatch(typeof(Punch), nameof(global::Punch.GetParryLookTarget))]
    [Postfix]
    static void Parry() => parried = true;
}
