namespace Jaket.Patches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(MinosArm))]
public class HandPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static bool Update() => LobbyController.Offline || LobbyController.IsOwner;

    [HarmonyPostfix]
    [HarmonyPatch("SlamDown")]
    static void SlamDown() { if (World.Hand && World.Hand.IsOwner) World.Hand.HandPos = 0; }

    [HarmonyPostfix]
    [HarmonyPatch("SlamLeft")]
    static void SlamLeft() { if (World.Hand && World.Hand.IsOwner) World.Hand.HandPos = 1; }

    [HarmonyPostfix]
    [HarmonyPatch("SlamRight")]
    static void SlamRight() { if (World.Hand && World.Hand.IsOwner) World.Hand.HandPos = 2; }
}
