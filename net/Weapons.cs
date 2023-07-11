namespace Jaket.Net;

using System.Collections.Generic;
using System.IO;
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
            if (revolver != null)
            {
                BulletPrefabs.Add(revolver.revolverBeam);
                BulletPrefabs.Add(revolver.revolverBeamSuper);
            }

            weapon.TryGetComponent<Shotgun>(out var shotgun);
            if (shotgun != null)
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
                BulletPrefabs.Add(launcher.cannonBall?.gameObject);
            }
        });

        // some variants are missing some projectiles
        BulletPrefabs.RemoveAll(bullet => bullet == null);
    }

    #region index

    public static int WeaponIndex(string name) => All.FindIndex(weapon => weapon.name == name);

    public static int CurrentWeaponIndex()
    {
        string name = GunControl.Instance.currentWeapon.name;
        return WeaponIndex(name.Substring(0, name.Length - "(Clone)".Length));
    }

    public static int BulletIndex(string name) => BulletPrefabs.FindIndex(bullet => bullet.name == name);

    public static int CopiedBulletIndex(string name) => BulletIndex(name.Substring(0, name.Length - "(Clone)".Length));

    #endregion
    #region weapons

    public static void TryDisable<T>(GameObject obj) where T : Behaviour
    {
        var component = obj.GetComponent<T>();
        if (component != null) component.enabled = false;
    }

    public static GameObject InstantinateWeapon(int index, Transform parent)
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

    #endregion
    #region bullets

    public static void WriteBullet(BinaryWriter w, GameObject bullet, bool hasRigidbody = false)
    {
        int index = Weapons.CopiedBulletIndex(bullet.name);
        if (index == -1) throw new System.Exception("Bullet index is -1!");

        w.Write(index);

        // position
        w.Write(bullet.transform.position.x);
        w.Write(bullet.transform.position.y);
        w.Write(bullet.transform.position.z);

        // rotation
        w.Write(bullet.transform.eulerAngles.x);
        w.Write(bullet.transform.eulerAngles.y);
        w.Write(bullet.transform.eulerAngles.z);

        w.Write(hasRigidbody);
        if (hasRigidbody)
        {
            var body = bullet.GetComponent<Rigidbody>();
            w.Write(body.velocity.x);
            w.Write(body.velocity.y);
            w.Write(body.velocity.z);
        }
        else
        {
            // the data size must be constant so that Networking can read it correctly
            w.Write(0f);
            w.Write(0f);
            w.Write(0f);
        }
    }

    public static byte[] WriteBullet(GameObject bullet, bool hasRigidbody = false) => Networking.Write(w => WriteBullet(w, bullet, hasRigidbody));

    public static void InstantinateBullet(BinaryReader r)
    {
        int index = r.ReadInt32();
        if (index == -1) return; // how?

        var obj = GameObject.Instantiate(BulletPrefabs[index]);
        obj.tag = "Net"; // needed to prevent object looping between client and server

        obj.transform.position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        obj.transform.eulerAngles = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        if (r.ReadBoolean()) obj.GetComponent<Rigidbody>().velocity = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }

    public static void InstantinateBullet(byte[] data) => Networking.Read(data, InstantinateBullet);

    #endregion
}