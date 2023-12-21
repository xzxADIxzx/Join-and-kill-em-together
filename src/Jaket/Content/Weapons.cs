namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

/// <summary> List of all weapons in the game and some useful methods. </summary>
public class Weapons
{
    /// <summary> List of prefabs of all weapons. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Loads all weapons for future use. </summary>
    public static void Load()
    {
        Prefabs.AddRange(GunSetter.Instance.revolverPierce.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.revolverRicochet.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.revolverTwirl.ToAssets());

        Prefabs.AddRange(GunSetter.Instance.shotgunGrenade.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.shotgunPump.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.shotgunRed.ToAssets());

        Prefabs.AddRange(GunSetter.Instance.nailMagnet.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.nailOverheat.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.nailRed.ToAssets());

        Prefabs.AddRange(GunSetter.Instance.railCannon.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.railHarpoon.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.railMalicious.ToAssets());

        Prefabs.AddRange(GunSetter.Instance.rocketBlue.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.rocketGreen.ToAssets());
        Prefabs.AddRange(GunSetter.Instance.rocketRed.ToAssets());
    }

    /// <summary> Finds the weapon type by the name. </summary>
    public static byte Type(GameObject weapon)
    {
        string name = weapon.name.Substring(0, weapon.name.IndexOf("("));
        return (byte)Prefabs.FindIndex(prefab => prefab.name == name);
    }
    public static byte Type() => GunControl.Instance.currentWeapon == null ? (byte)0xFF : Type(GunControl.Instance.currentWeapon);

    /// <summary> Spawns a weapon with the given type and assigns its parent transform. </summary>
    public static GameObject Instantiate(byte type, Transform parent)
    {
        var obj = Object.Instantiate(Prefabs[type], parent);

        // weapon prefabs are disabled and are located in the AlwaysOnTop layer
        obj.SetActive(true);
        FixLayer(obj.transform);

        // destroy revolver's and shotgun's hand
        Object.Destroy(obj.transform.GetChild(0).Find("RightArm")?.gameObject);
        if (obj.transform.childCount == 3) Object.Destroy(obj.transform.GetChild(2).Find("RightArm")?.gameObject);

        // destroy weapon's components
        Object.Destroy(obj.GetComponent<Revolver>());
        Object.Destroy(obj.GetComponent<Shotgun>());
        Object.Destroy(obj.GetComponent<Nailgun>());
        Object.Destroy(obj.GetComponent<Railcannon>());
        Object.Destroy(obj.GetComponent<RocketLauncher>());

        return obj;
    }

    /// <summary> Recursively iterates through all children of the transform and changes their layer to Outdoors. </summary>
    public static void FixLayer(Transform transform)
    {
        transform.gameObject.layer = 25;
        foreach (Transform child in transform) FixLayer(child);
    }
}
