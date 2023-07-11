namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;

/// <summary> List of all weapons in the game and some useful methods. </summary>
public class Weapons
{
    /// <summary> List of all weapons in the game. </summary>
    public static List<GameObject> All = new();
    /// <summary> Weapon bullet prefabs list. </summary>
    public static List<GameObject> BulletPrefabs = new();

    public static void Load()
    {
        All.AddRange(GunSetter.Instance.revolverPierce);
        All.AddRange(GunSetter.Instance.revolverRicochet);
        All.AddRange(GunSetter.Instance.revolverTwirl);

        All.AddRange(GunSetter.Instance.shotgunGrenade);
        All.AddRange(GunSetter.Instance.shotgunPump);
        All.AddRange(GunSetter.Instance.shotgunRed);

        All.AddRange(GunSetter.Instance.nailMagnet);
        All.AddRange(GunSetter.Instance.nailOverheat);
        All.AddRange(GunSetter.Instance.nailRed);

        All.AddRange(GunSetter.Instance.railCannon);
        All.AddRange(GunSetter.Instance.railHarpoon);
        All.AddRange(GunSetter.Instance.railMalicious);

        All.AddRange(GunSetter.Instance.rocketBlue);
        All.AddRange(GunSetter.Instance.rocketGreen);
        All.AddRange(GunSetter.Instance.rocketRed);

        All.ForEach(weapon =>
        {
            weapon.TryGetComponent<Revolver>(out var revolver);
            if (revolver != null) BulletPrefabs.Add(revolver.revolverBeam);

            weapon.TryGetComponent<Shotgun>(out var shotgun);
            if (revolver != null)
            {
                BulletPrefabs.Add(shotgun.bullet);
                BulletPrefabs.Add(shotgun.grenade);
            }

            weapon.TryGetComponent<Nailgun>(out var nailgun);
            if (nailgun != null)
            {
                BulletPrefabs.Add(nailgun.nail);
                BulletPrefabs.Add(nailgun.heatedNail);
                BulletPrefabs.Add(nailgun.magnetNail);
            }

            weapon.TryGetComponent<Railcannon>(out var railcannon);
            if (railcannon != null) BulletPrefabs.Add(railcannon.beam);

            weapon.TryGetComponent<RocketLauncher>(out var launcher);
            if (launcher != null)
            {
                BulletPrefabs.Add(launcher.rocket);
                BulletPrefabs.Add(launcher.cannonBall.gameObject);
            }
        });
    }

    public static int WeaponIndex(string name)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].name == name) return i;

        return -1;
    }

    public static int CurrentWeaponIndex()
    {
        string name = GunControl.Instance.currentWeapon.name;
        return WeaponIndex(name.Substring(0, name.Length - "(Clone)".Length));
    }

    public static void TryDisable<T>(GameObject obj) where T : Behaviour
    {
        var component = obj.GetComponent<T>();
        if (component != null) component.enabled = false;
    }

    public static GameObject Instantinate(int index, Transform parent)
    {
        var instance = GameObject.Instantiate(All[index], parent);
        instance.SetActive(true); // idk why, but weapon prefabs are disabled by default

        // destroy revolver's hand
        GameObject.Destroy(instance.transform.GetChild(0).Find("RightArm")?.gameObject);

        // disable weapon components
        TryDisable<Revolver>(instance);
        TryDisable<Shotgun>(instance);
        TryDisable<Nailgun>(instance);
        TryDisable<Railcannon>(instance);
        TryDisable<RocketLauncher>(instance);

        return instance;
    }
}