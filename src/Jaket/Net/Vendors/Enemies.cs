namespace Jaket.Net.Vendors;

using Sandbox;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;
using Jaket.World;

using static Entities;

/// <summary> Vendor responsible for enemies. </summary>
public class Enemies : Vendor
{
    public override void Load()
    {
        Fill(EntityType.Filth, EntityType.Sisyphus, GameAssets.Enemies);

        Events.Post
        (
            () => Prefabs[(byte)EntityType.Malicious],
            () => Prefabs[(byte)EntityType.Malicious] = Prefabs[(byte)EntityType.Malicious].ObjFind("Body")
        );

        Fill<Husk           >(EntityType.Filth,           EntityType.Soldier        );
        Fill<Swordsmachine  >(EntityType.Swordsmachine,   EntityType.Swordsmachine  );
        Fill<Earthmover     >(EntityType.SecuritySystem,  EntityType.Brain          );
        Fill<Malicious      >(EntityType.Malicious,       EntityType.Malicious      );
        Fill<Cerberus       >(EntityType.Cerberus,        EntityType.Cerberus       );

        Events.OnLoad += () =>
        {
            if (LobbyController.Offline) return;

            ResFind<SpiderLegsController  >().Each(IsReal, Imdt);
            ResFind<SpiderLegLines        >().Each(IsReal, Imdt);
            ResFind<EnemySpawnableInstance>().Each(IsReal, Imdt);
            ResFind<EnemyIdentifier       >().Each(IsReal, e =>
            {
                var type = Type(e.gameObject);
                e.Add<Entity.Identifier>(i => i.Type = type);
            });
        };
    }

    public override EntityType Type(GameObject obj)
    {
        if (obj && obj.HasIdentifier(out var id)) return id.Type.IsEnemy() ? id.Type : EntityType.None;

        if (Version.DEBUG) Log.Debug($"[ENTS] Missing an identifier of {(obj ? obj.name : "null")}");

        if (obj && obj.TryGetComponent(out EnemyIdentifier enemyId)) return Find
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

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsEnemy()) return null;

        var obj = Inst(Prefabs[(byte)type], position);

        return obj;
    }

    public override void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.HasAgent()) return;

        if (Gameflow.Mode.NoCommonEnemies()) Imdt(obj);
        else
        if (obj.activeSelf && obj.TryGetComponent(out EnemyIdentifier enemyId) && !enemyId.dead)
        {
            if (LobbyController.IsOwner || args[0])
            {
                var entity = Supply(type);

                entity.Owner = AccId;
                entity.Assign(obj.Add<Entity.Agent>(_ => { }));
                entity.Push();
            }
            else Imdt(obj);
        }
    }
}
