namespace Jaket.Harmony;

using UnityEngine;

using Jaket.Content;
using Jaket.Net;

public static class GunsPatch
{
    [StaticPatch(typeof(GunControl), nameof(GunControl.SwitchWeapon))]
    [Postfix]
    static void Switch() => Events.OnHandChange.Fire();

    [StaticPatch(typeof(GunControl), nameof(GunControl.ForceWeapon))]
    [Postfix]
    static void Forced() => Events.OnHandChange.Fire();

    [StaticPatch(typeof(GunColorGetter), nameof(GunColorGetter.UpdateColor))]
    [Prefix]
    static bool Colors(GunColorGetter __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [StaticPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [Prefix]
    static bool HudFix(WeaponIcon __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [StaticPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [Postfix]
    static void MatFix(WeaponIcon __instance, Renderer[] ___variationColoredRenderers)
    {
        var color = ColorBlindSettings.Instance.variationColors[(int)__instance.weaponDescriptor.variationColor];

        if (__instance.GetComponentInParent<Entity.Agent>() == null) return;

        if (__instance.TryGetComponent(out Revolver r)) r.screenMR?.Properties(b => b.SetColor("_Color", color), true);

        ___variationColoredRenderers.Each(r => r.Properties(b => b.SetColor("_EmissiveColor", color), true));
    }

    [DynamicPatch(typeof(GroundCheck), nameof(GroundCheck.UpdateState))]
    [Prefix]
    static void Shock(GroundCheck __instance)
    {
        if (__instance.superJumpChance <= 0f || __instance.superJumpChance >= Time.deltaTime || !NewMovement.Instance.stillHolding) return;

        Networking.Send(PacketType.Punch, 21, w =>
        {
            w.Id(AccId);
            w.Byte(0x01);

            w.Vector(__instance.transform.position);
            w.Float(NewMovement.Instance.slamForce);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught shockwave explosion");
    }

    [DynamicPatch(typeof(Punch), nameof(Punch.BlastCheck))]
    [Prefix]
    static void Blast(Punch __instance)
    {
        if (!__instance.heldAction.IsPressed()) return;

        Networking.Send(PacketType.Punch, 29, w =>
        {
            w.Id(AccId);
            w.Byte(0x02);

            w.Vector(CameraController.Instance.GetDefaultPos() + CameraController.Instance.transform.forward * 2f);
            w.Vector(CameraController.Instance.transform.localEulerAngles);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught blastwave explosion");
    }

    [DynamicPatch(typeof(Shotgun), nameof(Shotgun.Shoot))]
    [Prefix]
    static void PumpShotgun(Shotgun __instance)
    {
        if (__instance.variation != 1 || __instance.primaryCharge != 3) return;

        Networking.Send(PacketType.Punch, 29, w =>
        {
            w.Id(AccId);
            w.Byte(0x03);

            w.Vector(CameraController.Instance.transform.position + CameraController.Instance.transform.forward);
            w.Vector(CameraController.Instance.transform.localEulerAngles);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught shotgun explosion");
    }

    [DynamicPatch(typeof(ShotgunHammer), nameof(ShotgunHammer.ImpactEffects))]
    [Prefix]
    static void PumpHammer(ShotgunHammer __instance, bool ___forceWeakHit, int ___tier)
    {
        Networking.Send(PacketType.Punch, 29, w =>
        {
            w.Id(AccId);
            w.Byte((byte)(0xF0 + (__instance.primaryCharge << 2) + (___forceWeakHit ? 0 : ___tier)));

            w.Vector(CameraController.Instance.transform.position + CameraController.Instance.transform.forward * 2.5f);
            w.Vector(CameraController.Instance.transform.localEulerAngles);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught hammer explosion");
    }
}
