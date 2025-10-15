namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

using static Entities;

/// <summary> Vendor responsible for explosions and damage. </summary>
public class Damage : Vendor
{
    /// <summary> List of internal names of all melee damage types. </summary>
    public static readonly string[] Melee = { "punch", "heavypunch", "hook", "ground slam", "hammer", "chainsawzone" };

    public void Load()
    {
        EntityType counter = EntityType.Shockwave;
        GameAssets.Explosions.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });
        GameAssets.Particles.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Particle(w, p => Vendor.Prefabs[index] = p);
        });
    }

    public EntityType Type(GameObject obj) => EntityType.None;

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null) => null;

    public void Sync(GameObject obj, params bool[] args) { }
}
