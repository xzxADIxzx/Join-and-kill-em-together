namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;
using Jaket.World;

using static Entities;

/// <summary> Vendor responsible for enemies. </summary>
public class Enemies : Vendor
{
    public void Load()
    {
        EntityType counter = EntityType.Filth;
        GameAssets.Enemies.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });

        Events.Post
        (
            () => Vendor.Prefabs[(byte)EntityType.Malicious],
            () => Vendor.Prefabs[(byte)EntityType.Malicious] = Vendor.Prefabs[(byte)EntityType.Malicious].transform.Find("Body").gameObject
        );

        for (EntityType i = EntityType.Filth;          i <= EntityType.Soldier; i++) Vendor.Suppliers[(byte)i] = (id, type) => new Husk      (id, type);
        for (EntityType i = EntityType.SecuritySystem; i <= EntityType.Brain;   i++) Vendor.Suppliers[(byte)i] = (id, type) => new Earthmover(id, type);
    }

    public EntityType Type(GameObject obj)
    {
        if (obj?.TryGetComponent(out EnemyIdentifier enemyId) ?? false) return Vendor.Find
        (
            EntityType.Filth,
            EntityType.Sisyphus,
            p => p && p.TryGetComponent(out EnemyIdentifier e)
                   && e.enemyType        == enemyId.enemyType
                   && e.overrideFullName == enemyId.overrideFullName
                   && e.weakPoint?.name  == enemyId.weakPoint?.name
        );
        else return EntityType.None;
    }

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsEnemy()) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        return obj;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.GetComponent<Entity.Agent>()) return;

        if (Gameflow.Mode.NoCommonEnemies()) Imdt(obj);
        else
        if (obj.activeSelf && obj.TryGetComponent(out EnemyIdentifier enemyId) && !enemyId.dead)
        {
            if (LobbyController.IsOwner || args[0])
            {
                var entity = Supply(type);

                entity.Owner = AccId;
                entity.Assign(obj.AddComponent<Entity.Agent>());
                entity.Push();
            }
            else Imdt(obj);
        }
    }
}
