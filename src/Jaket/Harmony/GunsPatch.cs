namespace Jaket.Harmony;

using HarmonyLib;
using UnityEngine;

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
}
