namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for projectiles. </summary>
public class Projectiles : Vendor
{
    public override void Load()
    {
        Fill(EntityType.Shell, EntityType.ProjectileExpl, GameAssets.Projectiles);

        Fill<Shell          >(EntityType.Shell,           EntityType.Shell          );
        Fill<Core           >(EntityType.Core,            EntityType.Core           );
        Fill<Nail           >(EntityType.NailCommon,      EntityType.NailHeated     );
        Fill<Sawblade       >(EntityType.SawbladeCommon,  EntityType.SawbladeHeated );
        Fill<Magnet         >(EntityType.Magnet,          EntityType.Magnet         );
        Fill<Screwdriver    >(EntityType.Screwdriver,     EntityType.Screwdriver    );
        Fill<Rocket         >(EntityType.Rocket,          EntityType.Rocket         );
        Fill<Cannon         >(EntityType.Cannonball,      EntityType.Cannonball     );
        Fill<Shell          >(EntityType.ProjectileHell,  EntityType.ProjectileExpl );

        Events.OnTeamChange += () => Networking.Entities.Alive<Projectile>(p => p.UpdateIgnore());
    }

    public override EntityType Type(GameObject obj) => obj && obj.HasIdentifier(out var id) && id.Type.IsProjectile() ? id.Type : EntityType.None;

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsProjectile()) return null;

        var obj = Inst(Prefabs[(byte)type], position);

        return obj;
    }

    public override void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.HasAgent()) return;

        var entity = Supply(type);

        entity.Owner = AccId;
        entity.Assign(obj.Add<Entity.Agent>(_ => { }));
        entity.Push();
    }
}
