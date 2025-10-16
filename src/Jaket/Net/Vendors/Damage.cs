namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

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

    #region dealing

    /// <summary> Invokes the patch logic if the hitten collider is an entity. </summary>
    public bool Deal<T>(Component instance, Patch patch, Collider other = null, EnemyIdentifier eid = null) where T : Entity
    {
        if (instance.TryGetComponent(out Entity.Agent a) && a.Patron is T t)
        {
            if (!eid && !other.TryGetComponent(out eid)) eid = other.GetComponent<EnemyIdentifierIdentifier>()?.eid;
            if (!eid) return t.IsOwner;

            if (t.IsOwner && eid.TryGetComponent(out Entity.Agent target)) return patch(eid, target.Patron is RemotePlayer p && p.Team.Ally());
            return false;
        }
        else return true;
    }

    /// <summary> Patch logic to be executed when an entity is hitten. </summary>
    public delegate bool Patch(EnemyIdentifier eid, bool ally);

    #endregion
}
