namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for items. </summary>
public class Items : Vendor
{
    /// <summary> Template used for fishes. </summary>
    public GameObject FishTemplate;

    public void Load()
    {
        EntityType counter = EntityType.SkullBlue;
        GameAssets.Items.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });

        GameAssets.Prefab("Fishing/Fish Pickup Template.prefab", p => FishTemplate = p);

        GameAssets.Prefab("Levels/Hakita.prefab", p =>
        {
            Events.Post(() => Vendor.Prefabs[(byte)EntityType.Moon] != null, () => Events.Post(() => // damn Unity crashes w/o the second post
            {
                GameObject prefab;

                Keep(prefab = Make(EntityType.Moon));
                Dest(prefab.transform.Find("Fire").gameObject);
                Dest(prefab.transform.Find("Light").gameObject);
                Dest(prefab.GetComponent<Torch>());

                prefab.name = "Moon";
                prefab.GetComponent<ItemIdentifier>().itemType = ItemType.CustomKey1;
                Inst(p, prefab.transform).transform.localScale = Vector3.one * .1f;
            }));
        });

        Events.Post(() => ModAssets.V2        != null, () => Vendor.Prefabs[(byte)EntityType.V2       ] = ModAssets.V2       );
        Events.Post(() => ModAssets.V3        != null, () => Vendor.Prefabs[(byte)EntityType.V3       ] = ModAssets.V3       );
        Events.Post(() => ModAssets.xzxADIxzx != null, () => Vendor.Prefabs[(byte)EntityType.xzxADIxzx] = ModAssets.xzxADIxzx);
        Events.Post(() => ModAssets.Sowler    != null, () => Vendor.Prefabs[(byte)EntityType.Sowler   ] = ModAssets.Sowler   );

        for (EntityType i = EntityType.SkullBlue; i <= EntityType.BaitFace;  i++) Vendor.Suppliers[(byte)i] = (id, type) => new CommonItem(id, type);
        for (EntityType i = EntityType.FishFunny; i <= EntityType.FishBurnt; i++) Vendor.Suppliers[(byte)i] = (id, type) => new Fish      (id, type);
        for (EntityType i = EntityType.Hakita;    i <= EntityType.Sowler;    i++) Vendor.Suppliers[(byte)i] = (id, type) => new Plushie   (id, type);

        void SyncAll()
        {
            if (LobbyController.Offline) return;

            ResFind<ItemIdentifier>().Each(IsReal, i => Sync(i.gameObject, false));
            ResFind<ItemPlaceZone>().Each(IsReal, z =>
            {
                z.transform.parent = null;
                z.CheckItem();

                z.arenaStatuses.Each(s => s.currentStatus = 0);
                z.reverseArenaStatuses.Each(s => s.currentStatus = 0);
            });
        }
        Events.OnLoad += () => Events.Post(SyncAll);
        Events.OnLobbyEnter += () => Events.Post(SyncAll);
    }

    public EntityType Type(GameObject obj)
    {
        if (obj?.name.Contains("DevPlushie") ?? false)
        {
            if (obj.name == "DevPlushie (1)") return EntityType.Lenval;
            if (obj.name.Contains("(Clone)")) obj.name = obj.name[..^7];

            return Vendor.Find
            (
                EntityType.Hakita,
                EntityType.Sowler,
                p => p.name == obj.name
            );
        }
        if (obj?.TryGetComponent(out FishObjectReference fish) ?? false) return Vendor.Find
        (
            EntityType.FishFunny,
            EntityType.FishBurnt,
            p => p == fish.fishObject.worldObject
        );
        return (obj?.transform.childCount > 0 ? obj.transform.GetChild(obj.transform.childCount - 1).name : null) switch
        {
            "Arch"                 => EntityType.Moon,
            "Florp"                => EntityType.Florp,
            "Apple Bait (1)"       => EntityType.BaitApple,
            "Maurice Prop"         => EntityType.BaitFace,
            _                      => obj?.GetComponent<ItemIdentifier>().itemType switch
            {
                ItemType.SkullBlue => EntityType.SkullBlue,
                ItemType.SkullRed  => EntityType.SkullRed,
                ItemType.Soap      => EntityType.Soap,
                ItemType.Torch     => EntityType.Torch,
                _                  => EntityType.None
            }
        };
    }

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (!type.IsItem()) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        if (type.IsFish())
        {
            obj.transform.parent = Component<FishObjectReference>(Inst(FishTemplate, position), f =>
            {
                f.fishObject = ResFind<FishObject>().Find(o => o.worldObject == Vendor.Prefabs[(byte)type]);
            }).transform;
            obj.transform.localRotation = obj.transform.Find("../Dummy Object").localRotation; // it is some kind of template
        }

        return type.IsFish() ? obj.transform.parent.gameObject : obj;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.GetComponent<Entity.Agent>()) return;

        if (obj.activeSelf && obj.TryGetComponent(out ItemIdentifier itemId) && !itemId.infiniteSource)
        {
            // for some unknown reason this value is true for the museum plushies
            itemId.pickedUp = type.IsFish();

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
