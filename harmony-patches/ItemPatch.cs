namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.World;

[HarmonyPatch(typeof(ItemIdentifier), MethodType.Constructor)]
public class ItemPatch
{
    static void Prefix(ItemIdentifier __instance) => Events.Post(() => Items.SyncPlushy(__instance));
}