namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

/// <summary> List of all weapons in the game and some useful methods. </summary>
public class Weapons
{
    /// <summary> List of all weapons in the game. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Loads all weapons for future use. </summary>
    public static void Load()
    {
        Prefabs.AddRange(GunSetter.Instance.revolverPierce);
        Prefabs.AddRange(GunSetter.Instance.revolverRicochet);
        Prefabs.AddRange(GunSetter.Instance.revolverTwirl);

        Prefabs.AddRange(GunSetter.Instance.shotgunGrenade);
        Prefabs.AddRange(GunSetter.Instance.shotgunPump);
        Prefabs.AddRange(GunSetter.Instance.shotgunRed);

        Prefabs.AddRange(GunSetter.Instance.nailMagnet);
        Prefabs.AddRange(GunSetter.Instance.nailOverheat);
        Prefabs.AddRange(GunSetter.Instance.nailRed);

        Prefabs.AddRange(GunSetter.Instance.railCannon);
        Prefabs.AddRange(GunSetter.Instance.railHarpoon);
        Prefabs.AddRange(GunSetter.Instance.railMalicious);

        Prefabs.AddRange(GunSetter.Instance.rocketBlue);
        Prefabs.AddRange(GunSetter.Instance.rocketGreen);
        Prefabs.AddRange(GunSetter.Instance.rocketRed);
    }

    #region index

    /// <summary> Finds weapon index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds weapon index by the name of its clone. </summary>
    public static int CopiedIndex(string name) => Index(name.Substring(0, name.IndexOf("(Clone)")));

    /// <summary> Finds current weapon index. </summary>
    public static int CurrentIndex() => CopiedIndex(GunControl.Instance.currentWeapon.name);

    #endregion
    #region instantiation

    /// <summary> Creates a weapon with the given index and assigns its parent transform. </summary>
    public static GameObject Instantiate(int index, Transform parent)
    {
        var obj = Object.Instantiate(Prefabs[index], parent);
        obj.SetActive(true); // idk why, but weapon prefabs are disabled by default

        // by default, weapons are very strangely rotated
        obj.transform.eulerAngles = new Vector3(0f, 90f, 0f);

        // destroy revolver's hand
        Object.Destroy(obj.transform.GetChild(0).Find("RightArm")?.gameObject);

        // destroy weapon's components
        Object.Destroy(obj.GetComponent<Revolver>());
        Object.Destroy(obj.GetComponent<Shotgun>());
        Object.Destroy(obj.GetComponent<Nailgun>());
        Object.Destroy(obj.GetComponent<Railcannon>());
        Object.Destroy(obj.GetComponent<RocketLauncher>());

        return obj;
    }

    #endregion
}