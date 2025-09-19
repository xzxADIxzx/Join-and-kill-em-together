namespace Jaket.Net.Vendors;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

using static Entities;

/// <summary> Vendor responsible for weapons. </summary>
public class Weapons : Vendor
{
    public void Load()
    {
        EntityType counter = EntityType.RevolverBlue;
        GameAssets.Weapons.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });
    }

    public EntityType Type(GameObject obj) => Vendor.Find
    (
        EntityType.RevolverBlue,
        EntityType.RocketlRed,
        p => p.name.Length == obj?.name.Length - 7 && (obj?.name.Contains(p.name) ?? false)
    );

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsWeapon()) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], parent);

        obj.SetActive(true);
        obj.GetComponentsInChildren<Renderer>().Each(c => c.gameObject.layer = 24); // outdoors
        obj.GetComponentsInChildren<Canvas>().Each(c => c.gameObject.layer = 24);   // outdoors
        obj.GetComponentsInChildren<AudioSource>().Each(s => s.spatialBlend = 1f);  // surround audio

        foreach (var path in new string[] {

            "Revolver_Rerigged_Standard/RightArm",
            "Revolver_Rerigged_Standard/Armature/Upper Arm/Forearm/Hand/Revolver_Bone/ShootPoint",
            "Revolver_Rerigged_Alternate/RightArm",
            "Revolver_Rerigged_Alternate/Armature/Upper Arm/Forearm/Hand/Revolver_Bone/ShootPoint (1)",
            "ImpactHammer/Armature/Root/MotorSpinner/SpinSprite"

        }) Dest(obj.transform.Find(path)?.gameObject);

        foreach (var comp in new Type[] {

            typeof(Revolver),
            typeof(Shotgun),
            typeof(ShotgunHammer),
            typeof(Nailgun),
            typeof(Railcannon),
            typeof(RocketLauncher)

        }) Dest(obj.GetComponent(comp));

        return obj;
    }

    public void Sync(GameObject obj, params bool[] args) { }
}
