namespace Jaket.Content;

using System;
using System.Collections.Generic;
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
            if (LobbyController.Lobby != null) Events.Post(SyncAll);
        };
        Events.OnLobbyEntered += () => Events.Post(SyncAll);

        foreach (var name in GameAssets.Items) Prefabs.Add(GameAssets.Item(name).transform);
        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushy(name).transform);
    }

    /// <summary> Finds the entity type by item class and first/last child name. </summary>
    public static EntityType Type(ItemIdentifier itemId)
    {
        // items are divided into two types: regular and plushies
        if (itemId.name.StartsWith("DevPlushie"))
        {
            int index = Prefabs.FindIndex(prefab => prefab.GetChild(prefab.childCount - 1).name == itemId.transform.GetChild(itemId.transform.childCount - 1).name);
            return index == -1 ? EntityType.None : (EntityType.ItemOffset + index);
        }
        else return itemId.transform.GetChild(0).name switch
        {
            "Apple Bait" => EntityType.AppleBait,
            "Maurice Prop" => EntityType.SkullBait,
            _ => itemId.itemType switch
            {
                ItemType.SkullBlue => EntityType.BlueSkull,
                ItemType.SkullRed => EntityType.RedSkull,
                ItemType.Soap => EntityType.Soap,
                ItemType.Torch => EntityType.Torch,
                _ => EntityType.Florp
            }
        };
    }
    public static EntityType Type(Entity entity) => entity.ItemId == null ? EntityType.None : Type(entity.ItemId);

    /// <summary> Spawns an item with the given type. </summary>
    public static Item Instantiate(EntityType type) => GameObject.Instantiate(Prefabs[type - EntityType.ItemOffset].gameObject).AddComponent<Item>();

    /// <summary> Synchronizes the item between host and clients. </summary>
    public static void Sync(ItemIdentifier itemId)
    {
        if (LobbyController.Lobby == null || itemId == null || itemId.gameObject == null) return;

        // the item was created remotely, the item is a book or the item is a prefab
        if (itemId.name == "Net" || itemId.name.Contains("Book") || itemId.gameObject.scene.name == null) return;
        // sometimes the developer just deactivates the skulls instead of removing them
        if (!itemId.gameObject.activeSelf) return;
        // what did I do to deserve this?
        if (Array.Exists(GameAssets.ItemExceptions, ex => ex == itemId.name)) return;

        if (LobbyController.IsOwner)
            itemId.gameObject.AddComponent<Item>();
        else
            Tools.DestroyImmediate(itemId.gameObject);
    }

    /// <summary> Synchronizes all items in the level. </summary>
    public static void SyncAll()
    {
        List<ItemPlaceZone> altars = new(Tools.ResFind<ItemPlaceZone>());
        altars.RemoveAll(altar => altar.gameObject.scene.name == null);

        foreach (var zone in altars) zone.transform.SetParent(null);
        foreach (var item in Tools.ResFind<ItemIdentifier>()) Sync(item);
        foreach (var zone in altars)
        {
            // at level 5-3 there are altars that activate skulls in the mirror part of the level, but the client has these skulls destroyed
            if (zone.activateOnSuccess.Length > 0 && zone.activateOnSuccess[0] == null) zone.activateOnSuccess = new GameObject[] { zone.activateOnSuccess[1] };

            // re-linking skulls with altars
            if (zone.GetComponentInChildren<ItemIdentifier>() == null && zone.TryGetComponent<Collider>(out var col)) col.enabled = true;
        }
    }
}
