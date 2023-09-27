namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(MinosArm), "SlamDown")]
public class SlamDownPatch
{
    static void Postfix() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 0; }
}

[HarmonyPatch(typeof(MinosArm), "SlamLeft")]
public class SlamLeftPatch
{
    static void Postfix() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 1; }
}

[HarmonyPatch(typeof(MinosArm), "SlamRight")]
public class SlamRightPatch
{
    static void Postfix() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Hand != null) World.Instance.Hand.HandPos = 2; }
}
