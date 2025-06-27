namespace Jaket.Harmony;

using HarmonyLib;

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
}
