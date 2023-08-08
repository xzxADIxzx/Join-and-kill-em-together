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
        var button = Utils.Button("Join Jaket Discord", root, 0f, 0f, () => Application.OpenURL("https://discord.gg/USpt3hCBgn"), 24);

        // add a stroke to match the style
        var stroke = Utils.Image("Stroke", button.transform, 0f, 0f, 320f, 64f, Color.white).GetComponent<Image>();

        // make the stroke non-clickable and remove the fill
        stroke.raycastTarget = false;
        stroke.fillCenter = false;

        // the button is a little stormy
        button.transform.localPosition = new(0f, -128f, -20f);
        button.transform.localRotation = Quaternion.identity;
        button.transform.localScale = new(1f, 1f, 1f);

        // add ControllerPointer so that the button can be clicked
        button.AddComponent<ShopButton>(); // hacky
        Object.Destroy(button.GetComponent<ShopButton>());
    }
}
