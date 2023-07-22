namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;

[HarmonyPatch(typeof(Coin), "StartCheckingSpeed")] // for some reason, the coin has zero velocity in Start
public class CoinPatch
{
    static void Postfix(Coin __instance) => Bullets.Send(__instance.gameObject, true);
}

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance) => Bullets.Send(__instance.gameObject);
}

[HarmonyPatch(typeof(Nail), "Start")]
public class NailPatch
{
    static void Prefix(Nail __instance) => Bullets.Send(__instance.gameObject, true);
}

[HarmonyPatch(typeof(Harpoon), "Start")]
public class HarpoonPatch
{
    static void Prefix(Harpoon __instance) => Bullets.Send(__instance.gameObject, true);
}

[HarmonyPatch(typeof(Grenade), "Start")]
public class GrenadePatch
{
    static void Prefix(Grenade __instance) => Bullets.Send(__instance.gameObject);
}

[HarmonyPatch(typeof(Cannonball), "Start")]
public class CannonballPatch
{
    static void Prefix(Cannonball __instance) => Bullets.Send(__instance.gameObject, true);
}
