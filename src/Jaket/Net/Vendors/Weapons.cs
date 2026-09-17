namespace Jaket.Net.Vendors;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

using static Entities;

/// <summary> Vendor responsible for weapons. </summary>
public class Weapons : Vendor
{
    public override void Load()
    {
        Fill(EntityType.RevolverBlue, EntityType.RocketlRed, GameAssets.Weapons);
    }

    public override EntityType Type(GameObject obj) => obj && obj.HasIdentifier(out var id) && id.Type.IsWeapon() ? id.Type : EntityType.None;

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsWeapon()) return null;

        var obj = Inst(Prefabs[(byte)type], parent);

        obj.SetActive(true);
        obj.GetComponentsInChildren<Renderer   >().Each(c => c.gameObject.layer = 24); // outdoors
        obj.GetComponentsInChildren<Canvas     >().Each(c => c.gameObject.layer = 24); // outdoors
        obj.GetComponentsInChildren<AudioSource>().Each(c => c.spatialBlend     = 1f); // surround audio

        foreach (var path in new string[]
        {
            "Revolver_Rerigged_Standard/RightArm",
            "Revolver_Rerigged_Standard/Armature/Upper Arm/Forearm/Hand/Revolver_Bone/ShootPoint",
            "Revolver_Rerigged_Alternate/RightArm",
            "Revolver_Rerigged_Alternate/Armature/Upper Arm/Forearm/Hand/Revolver_Bone/ShootPoint (1)",
            "Nailgun New New/Armature/Main/Barrel_L/Barrel_L (1)",
            "Nailgun New New/Armature/Main/Barrel_R/Barrel_R (1)",
            "ImpactHammer/Armature/Root/MotorSpinner/SpinSprite",
        }
        ) Dest(obj.DefFind(path));

        foreach (var comp in new Type[]
        {
            typeof(Revolver),
            typeof(Shotgun),
            typeof(ShotgunHammer),
            typeof(Nailgun),
            typeof(Railcannon),
            typeof(RocketLauncher)
        }
        ) Dest(obj.GetComponent(comp));

        return obj;
    }

    public override void Sync(GameObject obj, params bool[] args) { }
}
