namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;

[HarmonyPatch(typeof(Grenade), "Start")]
public class GrenadePatch
{
    static void Prefix(Grenade __instance) => Bullets.Send(__instance.gameObject);
}
