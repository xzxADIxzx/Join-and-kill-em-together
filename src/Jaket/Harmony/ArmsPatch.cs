namespace Jaket.Harmony;

using UnityEngine;

using Jaket.Net;
using Jaket.Net.Types;

public static class ArmsPatch
{
    static EnemyIdentifier caught;

    [DynamicPatch(typeof(HookArm), nameof(HookArm.FixedUpdate))]
    [Postfix]
    static void Hook(HookArm __instance, HookState ___state, EnemyIdentifier ___caughtEid, bool ___lightTarget)
    {
        Networking.LocalPlayer.Hook = __instance.forcingFistControl ? __instance.hookPoint : Vector3.zero;

        if (___state == HookState.Pulling && ___caughtEid && ___lightTarget)
        {
            if (caught == ___caughtEid) return;
            if ((caught = ___caughtEid).HasEntity(out Enemy e)) e.TakeOwnage();
        }
        else caught = null;
    }

    static bool parried { get { var value = field; field = false; return value; } set; }

    [DynamicPatch(typeof(Punch), nameof(global::Punch.ActiveEnd))]
    [Postfix]
    static void Punch() => Entities.Players.Play(FistControl.Instance.currentPunch.type == FistType.Heavy ? 10 : parried ? 9 : 8);

    [DynamicPatch(typeof(Punch), nameof(global::Punch.GetParryLookTarget))]
    [Postfix]
    static void Parry() => parried = true;

    [DynamicPatch(typeof(Punch), nameof(global::Punch.BlastCheck))]
    [Postfix]
    static void Blast(Punch __instance)
    {
        if (__instance.heldAction.IsPressed())
        {
            Entities.Players.Play(11, 2f);
            if (Version.DEBUG) Log.Debug("[HARM] Caught blastwave explosion");
        }
    }
}
