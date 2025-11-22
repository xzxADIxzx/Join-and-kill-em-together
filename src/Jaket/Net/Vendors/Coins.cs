namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for coins. </summary>
public class Coins : Vendor
{
    public void Load()
    {
        byte type = (byte)EntityType.Coin;

        GameAssets.Prefab("Attacks and Projectiles/Coin.prefab", p => Vendor.Prefabs[type] = p);

        Vendor.Suppliers[type] = (id, type) => new TeamCoin(id, type);
    }

    public EntityType Type(GameObject obj) => obj.name == "Coin(Clone)" ? EntityType.Coin : EntityType.None;

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (type != EntityType.Coin) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        return obj;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.GetComponent<Entity.Agent>()) return;

        var entity = Supply(type);

        entity.Owner = AccId;
        entity.Assign(obj.AddComponent<Entity.Agent>());
        entity.Push();
    }
}
