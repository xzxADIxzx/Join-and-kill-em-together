namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;

[HarmonyPatch(typeof(Coin), "StartCheckingSpeed")] // for some reason, the bullet has zero velocity when calling Start
public class CoinPatch
{
    static void Postfix(Coin __instance) => Bullets.Send(__instance.gameObject, true);
}
