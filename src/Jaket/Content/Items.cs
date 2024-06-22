namespace Jaket.Content;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> List of all items in the game and some useful methods. </summary>
public class Items
{
    /// <summary> List of prefabs of all items. </summary>
    public static List<Transform> Prefabs = new();

    /// <summary> Loads all items for future use. </summary>
    public static void Load()
    {
        Events.OnLoaded += () =>
        {
            if (LobbyController.Online) Events.Post2(SyncAll);
        };
        Events.OnLobbyEntered += () => Events.Post2(SyncAll);

        foreach (var name in GameAssets.Items) Prefabs.Add(GameAssets.Item(name).transform);
        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushy(name).transform);
    }

    /// <summary> Finds the entity type by item class and first/last child name. </summary>
    public static EntityType Type(Entity entity)
    {
        var id = entity.ItemId;
        if (id == null) return EntityType.None;

        // items are divided into two types: regular and plushies
        if (id.name.StartsWith("DevPlushie"))
        {
            int index = Prefabs.FindIndex(prefab => prefab.GetChild(prefab.childCount - 1).name == id.transform.GetChild(id.transform.childCount - 1).name);
            return index == -1 ? EntityType.None : (EntityType.ItemOffset + index);
        }
        else return id.transform.GetChild(0).name switch
        {
            "Apple Bait" => EntityType.AppleBait,
            "Maurice Prop" => EntityType.SkullBait,
            _ => id.itemType switch
            {
                ItemType.SkullBlue => EntityType.BlueSkull,
                ItemType.SkullRed => EntityType.RedSkull,
                ItemType.Soap => EntityType.Soap,
                ItemType.Torch => EntityType.Torch,
                _ => EntityType.Florp
            }
        };
    }

    /// <summary> Spawns an item with the given type. </summary>
    public static Entity Instantiate(EntityType type) => Entities.Mark(Prefabs[type - EntityType.ItemOffset].gameObject).AddComponent<Item>();

    /// <summary> Synchronizes the item between network members. </summary>
    public static void Sync(ItemIdentifier itemId, bool single = true)
    {
        if (LobbyController.Offline || itemId == null || itemId.gameObject == null) return;

        // the item was created remotely, the item is a book or the item is a prefab
        if (itemId.name == "Net" || itemId.name.Contains("Book") || !Tools.IsReal(itemId)) return;
        // sometimes developers just deactivate skulls instead of removing them
        if (!itemId.gameObject.activeSelf || GameAssets.ItemExceptions.Contains(itemId.name)) return;

        if (LobbyController.IsOwner || single)
            itemId.gameObject.AddComponent<Item>();
        else
            Tools.DestroyImmediate(itemId.gameObject);
    }

    /// <summary> Synchronizes all items in the level. </summary>
    public static void SyncAll()
    {
        foreach (var item in Tools.ResFind<ItemIdentifier>()) Sync(item, false);
        foreach (var zone in Tools.ResFind<ItemPlaceZone>())
        {
            if (!Tools.IsReal(zone)) continue;
            zone.transform.SetParent(null);
            zone.CheckItem();
        }
    }
}
