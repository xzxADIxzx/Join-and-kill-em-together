namespace Jaket.HarmonyPatches;

using Discord;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.UI;

[HarmonyPatch(typeof(DiscordController), "SendActivity")]
public class DiscordControllerPatch
{
    static void Prefix(ref Activity ___cachedActivity)
    {
        // update the discord activity so everyone can know I've been working hard
        if (LobbyController.Lobby != null) ___cachedActivity.State = "Playing multiplayer via Jaket";
    }
}

[HarmonyPatch(typeof(ShopZone), "Start")]
public class ShopPatch
{
    static void Prefix(ShopZone __instance)
    {
        // patch only the most common shops
        if (__instance.gameObject.name != "Shop") return;

        // find tip box
        var root = __instance.transform.GetChild(1).GetChild(1).GetChild(0);

        // create button redirects to discord
        var button = UI.DiscordButton("Join Jaket Discord", root, 0f, 0f, size: 24).transform;

        // the button is a little stormy
        button.localPosition = new(0f, -128f, -20f);
        button.localRotation = Quaternion.identity;
        button.localScale = new(1f, 1f, 1f);

        foreach (Transform child in button.transform) child.localPosition = Vector3.zero;

        // add ControllerPointer so that the button can be clicked
        button.gameObject.AddComponent<ShopButton>(); // hacky
        Object.Destroy(button.GetComponent<ShopButton>());
    }
}
