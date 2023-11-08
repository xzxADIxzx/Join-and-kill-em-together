namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net.EntityTypes;
using Jaket.World;

[HarmonyPatch(typeof(ItemIdentifier), MethodType.Constructor)]
public class ItemSyncPatch
{
    static void Prefix(ItemIdentifier __instance) => Events.Post(() => Items.SyncPlushy(__instance));
}

[HarmonyPatch(typeof(ItemIdentifier), "PickUp")]
public class ItemPickPatch
{
    static void Prefix(ItemIdentifier __instance) => __instance.GetComponent<Item>()?.PickUp();
}
