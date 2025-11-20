namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for projectiles. </summary>
public class Projectiles : Vendor
{
    public void Load()
    {
        EntityType counter = EntityType.Core;
        GameAssets.Projectiles.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });

        for (EntityType i = EntityType.Core;           i <= EntityType.Core;           i++) Vendor.Suppliers[(byte)i] = (id, type) => new Core       (id, type);
        for (EntityType i = EntityType.NailCommon;     i <= EntityType.NailHeated;     i++) Vendor.Suppliers[(byte)i] = (id, type) => new Pin        (id, type);
        for (EntityType i = EntityType.SawbladeCommon; i <= EntityType.SawbladeHeated; i++) Vendor.Suppliers[(byte)i] = (id, type) => new Sawblade   (id, type);
        for (EntityType i = EntityType.Screwdriver;    i <= EntityType.Screwdriver;    i++) Vendor.Suppliers[(byte)i] = (id, type) => new Screwdriver(id, type);
        for (EntityType i = EntityType.Rocket;         i <= EntityType.Rocket;         i++) Vendor.Suppliers[(byte)i] = (id, type) => new Rocket     (id, type);
        for (EntityType i = EntityType.Cannonball;     i <= EntityType.Cannonball;     i++) Vendor.Suppliers[(byte)i] = (id, type) => new Cannon     (id, type);
    }

    public EntityType Type(GameObject obj) => Vendor.Find
    (
        EntityType.Core,
        EntityType.Cannonball,
        p => p.name.Length == obj?.name.Length - 7 && (obj?.name.Contains(p.name) ?? false)
    );

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsProjectile()) return null;

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
