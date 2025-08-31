namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for items. </summary>
public class Items : Vendor
{
    public void Load()
    {
        EntityType counter = EntityType.SkullBlue;
        GameAssets.Items.Each(w =>
        {
            byte index = (byte)counter++;
            GameAssets.Prefab(w, p => Vendor.Prefabs[index] = p);
        });

        for (EntityType i = EntityType.SkullBlue; i < EntityType.BaitFace;  i++) Vendor.Suppliers[(byte)i] = (id, type) => new CommonItem(id, type);
        for (EntityType i = EntityType.FishFunny; i < EntityType.FishBurnt; i++) Vendor.Suppliers[(byte)i] = (id, type) => new Fish      (id, type);
        for (EntityType i = EntityType.Hakita;    i < EntityType.Sowler;    i++) Vendor.Suppliers[(byte)i] = (id, type) => new Plushie   (id, type);

        void SyncAll()
        {
            if (LobbyController.Offline) return;

            ResFind<ItemIdentifier>().Each(i => Sync(i.gameObject, false));
            ResFind<ItemPlaceZone>().Each(IsReal, z =>
            {
                z.transform.parent = null;
                z.CheckItem();

                z.arenaStatuses.Each(s => s.currentStatus = 0);
                z.reverseArenaStatuses.Each(s => s.currentStatus = 0);
            });
        }
        Events.OnLoad += () => Events.Post2(SyncAll);
        Events.OnLobbyEnter += () => Events.Post2(SyncAll);
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
            p => p == fish.fishObject
        );
        return obj?.transform.GetChild(obj.transform.childCount - 1).name switch
        {
            "Arch"                 => EntityType.Moon,
            "Florp"                => EntityType.Florp,
            "Apple Bait (1)"       => EntityType.BaitApple,
            "Maurice Prop"         => EntityType.BaitFace,
            _ => obj.GetComponent<ItemIdentifier>().itemType switch
            {
                ItemType.SkullBlue => EntityType.SkullBlue,
                ItemType.SkullRed  => EntityType.SkullRed,
                ItemType.Soap      => EntityType.Soap,
                ItemType.Torch     => EntityType.Torch,
                _                  => EntityType.None
            }
        };
    }
}
