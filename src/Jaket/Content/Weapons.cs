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
        Events.OnLoad += () => {
        if (GunSetter.Instance == null || Prefabs.Count > 0) return;

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
    }; }

    /// <summary> Finds the weapon type by object name. </summary>
    public static byte Type()
    {
        var weap = GunControl.Instance.currentWeapon;
        if (weap == null) return 0xFF;

        var name = weap.name.Contains("(") ? weap.name.Substring(0, weap.name.IndexOf("(")) : weap.name;
        return (byte)Prefabs.FindIndex(prefab => prefab.name == name);
    }

    /// <summary> Spawns a weapon with the given type and assigns its parent transform. </summary>
    public static void Instantiate(byte type, Transform parent)
    {
        return;

        var obj = Inst(Prefabs[type], parent);

        // weapon prefabs are disabled and located in the AlwaysOnTop layer
        obj.SetActive(true);
        FixLayer(obj.transform);

        // destroy revolver's and shotgun's hand
        Dest(obj.transform.GetChild(0).Find("RightArm")?.gameObject);
        if (obj.transform.childCount == 3) Dest(obj.transform.GetChild(2).Find("RightArm")?.gameObject);

        // destroy weapon's components
        Dest(obj.GetComponent<Revolver>());
        Dest(obj.GetComponent<Shotgun>());
        Dest(obj.GetComponent<ShotgunHammer>());
        Dest(obj.GetComponent<Nailgun>());
        Dest(obj.GetComponent<Railcannon>());
        Dest(obj.GetComponent<RocketLauncher>());

        // make these annoying sounds quieter
        Dest(obj.transform.Find("ImpactHammer/Armature/Root/MotorSpinner/SpinSprite")?.gameObject);
        foreach (var source in obj.GetComponentsInChildren<AudioSource>())
        {
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    /// <summary> Recursively iterates through all children of the transform and changes their layer to Outdoors. </summary>
    public static void FixLayer(Transform transform)
    {
        transform.gameObject.layer = 25;
        transform.Each(FixLayer);
    }
}
