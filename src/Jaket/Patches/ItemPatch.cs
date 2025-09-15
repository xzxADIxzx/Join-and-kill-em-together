/*
namespace Jaket.ObsoletePatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch(typeof(FishCooker))]
public class FishPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static bool Cook(Collider other) => LobbyController.Offline || other.name == "Local";
}
*/
