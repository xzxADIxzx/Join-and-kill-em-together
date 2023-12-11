namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net.Types;

[HarmonyPatch(typeof(ItemIdentifier))]
public class ItemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor)]
    static void StartPatch(ItemIdentifier __instance) => Events.Post(() => Items.Sync(__instance));

    [HarmonyPrefix]
    [HarmonyPatch("PickUp")]
    static void PickPatch(ItemIdentifier __instance) => __instance.GetComponent<Item>()?.PickUp();
}
