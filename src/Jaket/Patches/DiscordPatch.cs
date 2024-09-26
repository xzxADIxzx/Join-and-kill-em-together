namespace Jaket.Patches;

using Discord;
using HarmonyLib;

using Jaket.Net;
using Jaket.UI;
using Jaket.UI.Elements;

[HarmonyPatch]
public class DiscordPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DiscordController), "SendActivity")]
    static void Activity(ref Activity ___cachedActivity)
    {
        // update the discord activity so everyone can know I've been working hard
        if (LobbyController.Online) ___cachedActivity.State = "Playing multiplayer via Jaket";
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShopZone), "Start")]
    static void Button(ShopZone __instance)
    {
        // patch only the most common shops
        if (__instance.name != "Shop") return;

        var button = UIB.DiscordButton("Join Jaket Discord", __instance.transform.GetChild(1).GetChild(1).GetChild(0));
        button.transform.localPosition = new(0f, -128f, -20f); // the button is a little stormy

        // add ControllerPointer so that the button can be clicked
        Tools.Destroy(button.gameObject.AddComponent<ShopButton>()); // hacky

        // add a customization element to allow players to choose their appearance
        if (__instance.transform.up.x == 0 && __instance.transform.up.z == 0) __instance.transform.Find("Canvas").gameObject.AddComponent<Customization>();

        // simplify the use of the customization element
        Tools.Set("angleLimit", __instance, 90f);
    }
}
