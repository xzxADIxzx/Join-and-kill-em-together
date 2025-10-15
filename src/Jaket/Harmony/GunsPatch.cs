namespace Jaket.Harmony;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

public static class GunsPatch
{
    [HarmonyPatch(typeof(GunControl), nameof(GunControl.SwitchWeapon))]
    [HarmonyPostfix]
    static void Switch() => Events.OnHandChange.Fire();

    [HarmonyPatch(typeof(GunControl), nameof(GunControl.ForceWeapon))]
    [HarmonyPostfix]
    static void Forced() => Events.OnHandChange.Fire();

    [HarmonyPatch(typeof(GunColorGetter), nameof(GunColorGetter.UpdateColor))]
    [HarmonyPrefix]
    static bool Colors(GunColorGetter __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [HarmonyPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [HarmonyPrefix]
    static bool HudFix(WeaponIcon __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [HarmonyPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [HarmonyPostfix]
    static void MatFix(WeaponIcon __instance, Renderer[] ___variationColoredRenderers)
    {
        var color = ColorBlindSettings.Instance.variationColors[(int)__instance.weaponDescriptor.variationColor];

        if (__instance.GetComponentInParent<Entity.Agent>() == null) return;

        if (__instance.TryGetComponent(out Revolver r)) r.screenMR?.Properties(b => b.SetColor("_Color", color), true);

        ___variationColoredRenderers.Each(r => r.Properties(b => b.SetColor("_EmissiveColor", color), true));
    }

    [HarmonyPatch(typeof(GroundCheck), "Update")]
    [HarmonyPrefix]
    static void Shock(GroundCheck __instance)
    {
        if (LobbyController.Offline || __instance.superJumpChance <= 0f || __instance.superJumpChance >= Time.deltaTime || !NewMovement.Instance.stillHolding) return;

        Networking.Send(PacketType.Punch, 21, w =>
        {
            w.Id(AccId);
            w.Byte(0x01);

            w.Vector(__instance.transform.position);
            w.Float(NewMovement.Instance.slamForce);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught shockwave explosion");
    }

    [HarmonyPatch(typeof(Punch), "BlastCheck")]
    [HarmonyPrefix]
    static void Blast(bool ___holdingInput)
    {
        if (LobbyController.Offline || !___holdingInput) return;

        Networking.Send(PacketType.Punch, 29, w =>
        {
            w.Id(AccId);
            w.Byte(0x02);

            w.Vector(CameraController.Instance.GetDefaultPos() + CameraController.Instance.transform.forward * 2f);
            w.Vector(CameraController.Instance.transform.localEulerAngles);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught blastwave explosion");
    }

    [HarmonyPatch(typeof(Shotgun), "Shoot")]
    [HarmonyPrefix]
    static void PumpShotgun(Shotgun __instance)
    {
        if (LobbyController.Offline || __instance.variation != 1 || __instance.primaryCharge != 3) return;

        Networking.Send(PacketType.Punch, 29, w =>
        {
            w.Id(AccId);
            w.Byte(0x03);

            w.Vector(CameraController.Instance.transform.position + CameraController.Instance.transform.forward);
            w.Vector(CameraController.Instance.transform.localEulerAngles);
        });
        if (Version.DEBUG) Log.Debug("[GUNS] Caught shotgun explosion");
    }

    [HarmonyPatch(typeof(ShotgunHammer), "ImpactEffects")]
    [HarmonyPrefix]
    static void PumpHammer(ShotgunHammer __instance, bool ___forceWeakHit, int ___tier)
    {
        if (LobbyController.Offline) return;

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
