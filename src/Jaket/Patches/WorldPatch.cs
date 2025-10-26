namespace Jaket.ObsoletePatches;

using HarmonyLib;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;
/*
[HarmonyPatch]
public class ActionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalDoor), nameof(FinalDoor.Open))]
    static void OpenDoor(FinalDoor __instance)
    {
        if (LobbyController.Online) World.SyncAction(__instance, 3);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    static void OpenCase(Door __instance)
    {
        var n = __instance.name;
        if (LobbyController.Online && LobbyController.IsOwner &&
           (n.Contains("Glass") || n.Contains("Cover") ||
            n.Contains("Skull") || n.Contains("Quake") ||
            Scene == "Level 3-1" || __instance.transform.parent?.parent?.name == "MazeWalls")) World.SyncAction(__instance, 4);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.SimpleOpenOverride))]
    static void OpenSpec(Door __instance)
    {
        if (LobbyController.Online && __instance.name == "BayDoor") World.SyncAction(__instance, 4);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BloodFiller), "FullyFilled")]
    static void FillBlood(BloodFiller __instance)
    {
        if (LobbyController.Online && LobbyController.IsOwner) World.SyncAction(__instance, 6);
    }
}
*/
[HarmonyPatch(typeof(IntermissionController))]
public class LovelyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static void Name(IntermissionController __instance)
    {
        if (LobbyController.Online) Votes.Name(__instance.GetComponent<Text>(), ref __instance.preText);
    }
}
