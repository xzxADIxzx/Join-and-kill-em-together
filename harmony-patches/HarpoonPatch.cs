namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;

[HarmonyPatch(typeof(Harpoon), "Start")]
public class HarpoonPatch
{
    static void Prefix(Harpoon __instance) => Bullets.Send(__instance.gameObject, true);
}
