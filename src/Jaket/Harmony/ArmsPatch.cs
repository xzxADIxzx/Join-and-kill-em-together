namespace Jaket.Harmony;

using System.Collections;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

public static class ArmsPatch
{
    const float PARRY_PAUSE = .25f;

    static EnemyIdentifier caught;
    static Animator pausedAnimator;
    static Coroutine resumeCoroutine;
    static float previousAnimatorSpeed;

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

    [DynamicPatch(typeof(Punch), nameof(global::Punch.Parry))]
    [Postfix]
    static void PausePunch(global::Punch __instance)
    {
        var animator = __instance.anim;
        if (!animator) return;

        if (resumeCoroutine != null) Plugin.Instance.StopCoroutine(resumeCoroutine);
        if (pausedAnimator != animator)
        {
            ResumePunch();
            pausedAnimator = animator;
            previousAnimatorSpeed = animator.speed;
        }

        animator.speed = 0f;
        resumeCoroutine = Plugin.Instance.StartCoroutine(ResumePunchLater());
    }

    static IEnumerator ResumePunchLater()
    {
        yield return new WaitForSecondsRealtime(PARRY_PAUSE);
        ResumePunch();
    }

    static void ResumePunch()
    {
        if (pausedAnimator) pausedAnimator.speed = previousAnimatorSpeed;
        pausedAnimator = null;
        resumeCoroutine = null;
    }
}
