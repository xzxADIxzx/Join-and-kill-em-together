namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch(typeof(ItemIdentifier))]
public class ItemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor)]
    static void Spawn(ItemIdentifier __instance) => Events.Post(() => Items.Sync(__instance));

    [HarmonyPrefix]
    [HarmonyPatch("PickUp")]
    static void PickUp(ItemIdentifier __instance) => __instance.GetComponent<Item>()?.PickUp();
}

[HarmonyPatch(typeof(FishCooker))]
public class FishPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static bool Cook(Collider other) => LobbyController.Offline || other.name == "Local";
}

[HarmonyPatch(typeof(ItemTrigger))]
public class BinPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static bool Death(ItemTrigger __instance, Collider other)
    {
        if (Tools.Scene != "CreditsMuseum2" || __instance.targetType != ItemType.CustomKey1 || !other || !other.transform.parent) return true;

        if (other.transform.parent.name == "adi") return PlushieAdi(__instance, other);
        if (other.transform.parent.name == "sowler") return PlushieSowler(__instance, other);
        return true;
    }

    static bool PlushieAdi(ItemTrigger trigger, Collider col)
    {
        GameObject Copy(string path) => Tools.Instantiate(Tools.ObjFind("SORT ME").transform.Find(path).gameObject, Vector3.zero);

        var black = Copy("OBJECT Activator Gianni world enemies/time of day changer");
        var white = Copy("time of day reverser");
        var music = Tools.ObjFind("Music/Music Player");

        var gif = trigger.onEvent.toActivateObjects[0];
        if (gif.TryGetComponent(out RandomPitch pitch) && gif.TryGetComponent(out ObjectActivator act))
        {
            pitch.defaultPitch = .2f;
            act.delay = 8f;

            black.SetActive(true);
            white.SetActive(false);
            music.SetActive(false);

            act.events.onActivate.AddListener(() =>
            {
                pitch.defaultPitch = 1f;
                act.delay = 1f;

                black.SetActive(false);
                white.SetActive(true);
                music.SetActive(true);
            });

            // prevent double activation
            col.transform.parent.name = "x-x";
        }
        return true;
    }

    static bool PlushieSowler(ItemTrigger trigger, Collider col)
    {
        GameAssets.GabLine("gab_Intro1d", clip => AudioSource.PlayClipAtPoint(clip, trigger.transform.position));

        var cam = CameraController.Instance.transform;
        var owl = col.transform.parent.parent;

        owl.position = cam.position - cam.forward with { y = 0f } * 8f;
        owl.LookAt(cam);

        return false;
    }
}
