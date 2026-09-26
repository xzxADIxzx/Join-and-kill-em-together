namespace Jaket.Harmony;

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
    static bool HudFix(GunColorGetter __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [StaticPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [Prefix]
    static bool HudFix(WeaponIcon __instance) => __instance.GetComponentInParent<Entity.Agent>() == null;

    [StaticPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    [Postfix]
    static void MatFix(WeaponIcon __instance)
    {
        var color = cb.variationColors[__instance.variationColor];

        if (__instance.GetComponentInParent<Entity.Agent>() == null) return;

        if (__instance.TryGetComponent(out Revolver r)) r.screenMR?.Set(p => p.SetColor("_Color", color));

        __instance.variationColoredRenderers.Each(r => r.Set(p => p.SetColor("_EmissiveColor", color)));
    }

    [DynamicPatch(typeof(Shotgun), nameof(global::Shotgun.Shoot))]
    [Prefix]
    static void Shotgun(Shotgun __instance)
    {
        if (__instance.variation == 1 && __instance.primaryCharge == 3)
        {
            Entities.Players.Play(12, 1f);
            if (Version.DEBUG) Log.Debug("[HARM] Caught shotgun explosion");
        }
    }

    [DynamicPatch(typeof(ShotgunHammer), nameof(ShotgunHammer.ImpactEffects))]
    [Prefix]
    static void Hammer(ShotgunHammer __instance)
    {
        Entities.Players.Play(0xF0 + (__instance.primaryCharge << 2) + (__instance.forceWeakHit ? 0 : __instance.tier), 2.5f);
        if (Version.DEBUG) Log.Debug("[HARM] Caught hammer explosion");
    }
}
