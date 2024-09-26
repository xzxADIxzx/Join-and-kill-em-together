namespace Jaket.Content;

using HarmonyLib;
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
    public static List<GameObject> Prefabs = new();

    /// <summary> Loads all items for future use. </summary>
    public static void Load()
    {
        Events.OnLoaded += () =>
        {
            if (LobbyController.Online) Events.Post2(SyncAll);
        };
        Events.OnLobbyEntered += () => Events.Post2(SyncAll);

        foreach (var name in GameAssets.Items) Prefabs.Add(GameAssets.Item(name));
        foreach (var name in GameAssets.Baits) Prefabs.Add(GameAssets.Bait(name));
        foreach (var name in GameAssets.Fishes) Prefabs.Add(GameAssets.Fish(name));
        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushie(name));
    }

    /// <summary> Finds the entity type by item class and first/last child name. </summary>
    public static EntityType Type(ItemIdentifier id)
    {
        if (id == null) return EntityType.None;

        if (id.name.StartsWith("Dev"))
        {
            if (id.name == "DevPlushie (1)") return EntityType.Lenval; // for God's sake, tell my why?!
            if (id.name.Contains("(Clone)")) id.name = id.name.Substring(0, id.name.IndexOf("(Clone)")).Trim();

            int index = Prefabs.FindIndex(prefab => prefab.name == id.name);
            return index == -1 ? EntityType.None : (EntityType.ItemOffset + index);
        }
        if (id.TryGetComponent(out FishObjectReference fish))
        {
            int index = FishManager.Instance.recognizedFishes.Keys.ToList().IndexOf(fish.fishObject);
            return index == -1 ? EntityType.BurntStuff : (EntityType.FishOffset + index + 2);
        }
        return id.transform.GetChild(0).name switch
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
    public static Entity Instantiate(EntityType type)
    {
        var fsh = type.IsFish() && type != EntityType.AppleBait && type != EntityType.SkullBait;
        var obj = fsh
            ? Entities.Mark(GameAssets.FishTemplate())
            : Entities.Mark(Prefabs[type - EntityType.ItemOffset]);

        // prefabs of fishes do not contain anything except the model of the fish
        if (fsh)
        {
            Tools.Instantiate(Prefabs[type - EntityType.ItemOffset], obj.transform).transform.localPosition = Vector3.zero;
            obj.AddComponent<FishObjectReference>();
        }

        return obj.AddComponent<Item>();
    }

    /// <summary> Synchronizes the item between network members. </summary>
    public static void Sync(ItemIdentifier itemId, bool single = true)
    {
        if (LobbyController.Offline || itemId == null || itemId.gameObject == null) return;

        // the item is already synced, the item is a book or the item is a prefab
        if (itemId.name == "Net" || itemId.name == "Local" || itemId.name.Contains("Book") || !Tools.IsReal(itemId) || itemId.infiniteSource) return;
        // sometimes developers just deactivate skulls instead of removing them
        if (!itemId.gameObject.activeSelf || GameAssets.ItemExceptions.Contains(itemId.name)) return;

        // somewhy this value is true for the plushies in the museum
        itemId.pickedUp = false;

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

            zone.arenaStatuses.Do(s => s.currentStatus = 0);
            zone.reverseArenaStatuses.Do(s => s.currentStatus = 0);
        }
    }
}

/// <summary> Extension class that allows you to get item data. </summary>
public static class ItemExtensions
{
    /// <summary> Whether the item is placed on an altar. </summary>
    public static bool Placed(this ItemIdentifier itemId) => itemId.transform.parent?.gameObject.layer == 22; // item layer
}
