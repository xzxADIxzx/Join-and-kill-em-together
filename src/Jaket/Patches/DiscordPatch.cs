namespace Jaket.Patches;

using Discord;
using HarmonyLib;
using UnityEngine;

using Jaket.Net;
using Jaket.UI;

[HarmonyPatch]
public class DiscordPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DiscordController), "SendActivity")]
    static void Activity(ref Activity ___cachedActivity)
    {
        // update the discord activity so everyone can know I've been working hard
        if (LobbyController.Lobby != null) ___cachedActivity.State = "Playing multiplayer via Jaket";
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShopZone), "Start")]
    static void Button(ShopZone __instance)
    {
        // patch only the most common shops
        if (__instance.gameObject.name != "Shop") return;

        var root = __instance.transform.GetChild(1).GetChild(1).GetChild(0);
        var button = UI.DiscordButton("Join Jaket Discord", root, 0f, 0f, size: 24).transform;

        // the button is a little stormy
        button.localPosition = new(0f, -128f, -20f);
        button.localRotation = Quaternion.identity;
        button.localScale = new(1f, 1f, 1f);

        foreach (Transform child in button.transform) child.localPosition = Vector3.zero;

        // add ControllerPointer so that the button can be clicked
        Tools.Destroy(button.gameObject.AddComponent<ShopButton>()); // hacky
    }
}
