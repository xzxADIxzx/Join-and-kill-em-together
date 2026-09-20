namespace Jaket.Harmony;

using UnityEngine;

using Jaket.Net;

public static class LegsPatch
{
    [DynamicPatch(typeof(NewMovement), nameof(NewMovement.Jump))]
    [Prefix]
    static void Jump(NewMovement __instance)
    {
        if (!__instance.modNoJump) Entities.Players.Play(0);
    }

    [DynamicPatch(typeof(NewMovement), nameof(NewMovement.WallJump))]
    [Prefix]
    static void Wall(NewMovement __instance)
    {
        Entities.Players.Play(1 + __instance.currentWallJumps);
    }

    [DynamicPatch(typeof(NewMovement), nameof(NewMovement.CheckLanding))]
    [Prefix]
    static void Land(NewMovement __instance)
    {
        if (__instance.gc.onGround && __instance.fallSpeed < 0)
        {
            if (__instance.fallSpeed > -50f)
                Entities.Players.Play(4);
            else
                Entities.Players.Play(5, __instance.gc.transform.position, -__instance.transform.up);
        }
    }

    [DynamicPatch(typeof(GroundCheck), nameof(GroundCheck.UpdateState))]
    [Prefix]
    static void Slam(GroundCheck __instance)
    {
        if (__instance.superJumpChance > 0f && __instance.superJumpChance < Time.deltaTime && NewMovement.Instance.stillHolding)
        {
            Entities.Players.Play(6, __instance.transform.position, -__instance.transform.up * NewMovement.Instance.slamForce);
            if (Version.DEBUG) Log.Debug("[HARM] Caught shockwave explosion");
        }
    }

    [DynamicPatch(typeof(NewMovement), nameof(NewMovement.TryDash))]
    [Prefix]
    static void Dash(NewMovement __instance)
    {
        if (!__instance.modNoDashSlide && __instance.boostCharge > 100f) Entities.Players.Play(7);
    }
}
