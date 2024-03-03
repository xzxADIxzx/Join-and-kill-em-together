namespace Jaket.Patches;

using HarmonyLib;

using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(MinotaurChase))]
public class MinotaurPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("IntroEnd")]
    static void Intro(MinotaurChase __instance) => __instance.enabled = LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPostfix]
    [HarmonyPatch("HammerSwing")]
    static void Hammer() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Minotaur != null) World.Instance.Minotaur.Attack = 0; }

    [HarmonyPostfix]
    [HarmonyPatch("MeatThrow")]
    static void Meat() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Minotaur != null) World.Instance.Minotaur.Attack = 1; }

    [HarmonyPostfix]
    [HarmonyPatch("HandSwing")]
    static void Hand() { if (LobbyController.Lobby != null && LobbyController.IsOwner && World.Instance.Minotaur != null) World.Instance.Minotaur.Attack = 2; }
}
