namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;

// TODO move all network patches into dedicated class
[HarmonyPatch(typeof(Harpoon), "Start")]
public class HarpoonPatch
{
    static void Prefix(Harpoon __instance) => Bullets.Send(__instance.gameObject, true);
}
