namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for items. </summary>
public class Items : Vendor
{
    public override void Load()
    {
        Fill(EntityType.SkullBlue, EntityType.V1, GameAssets.Items);

        Events.Post(() => ModAssets.Moon,      () => Prefabs[(byte)EntityType.Moon      ] = ModAssets.Moon     );
        Events.Post(() => ModAssets.Can,       () => Prefabs[(byte)EntityType.SnackCan  ] = ModAssets.Can      );
        Events.Post(() => ModAssets.Chips,     () => Prefabs[(byte)EntityType.SnackChips] = ModAssets.Chips    );
        Events.Post(() => ModAssets.Bar,       () => Prefabs[(byte)EntityType.SnackBar  ] = ModAssets.Bar      );
        Events.Post(() => ModAssets.Soda,      () => Prefabs[(byte)EntityType.SnackSoda ] = ModAssets.Soda     );
        Events.Post(() => ModAssets.V2,        () => Prefabs[(byte)EntityType.V2        ] = ModAssets.V2       );
        Events.Post(() => ModAssets.V3,        () => Prefabs[(byte)EntityType.V3        ] = ModAssets.V3       );
        Events.Post(() => ModAssets.xzxADIxzx, () => Prefabs[(byte)EntityType.xzxADIxzx ] = ModAssets.xzxADIxzx);
        Events.Post(() => ModAssets.Sowler,    () => Prefabs[(byte)EntityType.Sowler    ] = ModAssets.Sowler   );

        Fill<CommonItem     >(EntityType.SkullBlue,       EntityType.BaitFace       );
        Fill<Fish           >(EntityType.FishFunny,       EntityType.FishBurnt      );
        Fill<CommonItem     >(EntityType.SnackCan,        EntityType.SnackSoda      );
        Fill<Plushie        >(EntityType.Hakita,          EntityType.Sowler         );

        Events.OnLoad += () => Events.Post(() => Events.Post(() =>
        {
            if (LobbyController.Offline) return;

            ResFind<ItemIdentifier>().Each(IsReal, i => Sync(i.gameObject, false));
            ResFind<ItemPlaceZone>().Each(IsReal, z =>
            {
                z.transform.parent = null;
                z.CheckItem();

                z.arenaStatuses       .Each(s => s.currentStatus = 0);
                z.reverseArenaStatuses.Each(s => s.currentStatus = 0);
            });
        }));
    }

    public override EntityType Type(GameObject obj)
    {
        if (obj && obj.HasIdentifier(out var id)) return id.Type.IsItem() ? id.Type : EntityType.None;

        if (Version.DEBUG) Log.Debug($"[ENTS] Missing an identifier of {(obj ? obj.name : "null")}");

        if (obj && obj.name.Contains("DevPlushie")) return Find
        (
            EntityType.Hakita,
            EntityType.Sowler,
            p => p.name == obj.name || p.name == obj.name[..^7]
        );
        if (obj && obj.TryGetComponent(out FishObjectReference fish)) return Find
        (
            EntityType.FishFunny,
            EntityType.FishBurnt,
            p => p == fish.fishObject.worldObject
        );
        return (obj && obj.transform.childCount > 0 ? obj.transform.GetChild(obj.transform.childCount - 1).name : null) switch
        {
            "Arch"                 => EntityType.Moon,
            "Florp"                => EntityType.Florp,
            "Apple Bait (1)"       => EntityType.BaitApple,
            "Maurice Prop"         => EntityType.BaitFace,
            _                      => (obj && obj.TryGetComponent(out ItemIdentifier itemId) ? itemId.itemType : ItemType.None) switch
            {
                ItemType.SkullBlue => EntityType.SkullBlue,
                ItemType.SkullRed  => EntityType.SkullRed,
                ItemType.Soap      => EntityType.Soap,
                ItemType.Torch     => EntityType.Torch,
                _                  => EntityType.None
            }
        };
    }

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsItem()) return null;

        var obj = Inst(Prefabs[(byte)type], position);

        if (type.IsFish())
        {
            obj.transform.parent = Inst(ModAssets.Template, position).Add<FishObjectReference>(f =>
            {
                f.fishObject = ResFind<FishObject>().Find(o => o.worldObject == Prefabs[(byte)type]);
            }
            ).transform;
            obj.transform.localRotation = obj.DefFind("../Dummy Object").localRotation; // it is some sort of template
            obj = obj.transform.parent.gameObject;
        }

        return obj;
    }

    public override void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.HasAgent()) return;

        if (obj.activeSelf && obj.TryGetComponent(out ItemIdentifier itemId) && !itemId.infiniteSource)
        {
            // for some unknown reason this value is true for the museum plushies
            itemId.pickedUp = type.IsFish();

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
