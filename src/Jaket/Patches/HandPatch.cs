namespace Jaket.Patches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(MinosArm))]
public class HandPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static bool Update() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPostfix]
    [HarmonyPatch("SlamDown")]
    static void SlamDown() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 0; }

    [HarmonyPostfix]
    [HarmonyPatch("SlamLeft")]
    static void SlamLeft() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 1; }

    [HarmonyPostfix]
    [HarmonyPatch("SlamRight")]
    static void SlamRight() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 2; }
}
