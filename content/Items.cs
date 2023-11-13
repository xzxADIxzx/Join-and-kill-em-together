namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Net.EntityTypes;
using Jaket.World;

/// <summary> List of all items & plushies in the game and some useful methods. </summary>
public class Items
{
    /// <summary> List of all items & plushies in the game. </summary>
    public static List<Transform> Prefabs = new();

    /// <summary> Loads all items & plushies for future use. </summary>
    public static void Load()
    {
        // find and synchronize all the items & plushies on the level
        Events.OnLoaded += () => Events.Post(SyncAll);

        foreach (var name in GameAssets.Items) Prefabs.Add(GameAssets.Item(name).transform);
        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushy(name).transform);
    }

    #region index

    /// <summary> Finds item index by identifier. </summary>
    public static int ItemIndex(ItemIdentifier itemId) => itemId.transform.GetChild(0).name switch
    {
        "Apple Bait" => (int)EntityType.AppleBait,
        "Maurice Prop" => (int)EntityType.SkullBait,
        _ => itemId.itemType switch
        {
            ItemType.SkullBlue => (int)EntityType.BlueSkull,
            ItemType.SkullRed => (int)EntityType.RedSkull,
            ItemType.Soap => (int)EntityType.Soap,
            ItemType.Torch => (int)EntityType.Torch,
            _ => (int)EntityType.Florp
        }
    };

    /// <summary> Finds plushy index by its children. </summary>
    public static int PlushyIndex(Transform plushy) => plushy.childCount == 0
        ? -1
        : Prefabs.FindIndex(prefab => prefab.GetChild(prefab.childCount - 1).name == plushy.GetChild(plushy.childCount - 1).name);

    #endregion
    #region instantiation

    /// <summary> Spawns an item / plushy with the given type. </summary>
    public static Item Instantiate(EntityType type) => Object.Instantiate(Prefabs[(int)type - 35].gameObject).AddComponent<Item>();

    #endregion
    #region sync

    /// <summary> Synchronizes all items & plushies in the level. </summary>
    public static void SyncAll()
    {
        foreach (var item in Object.FindObjectsOfType<ItemIdentifier>()) Sync(item);
    }

    /// <summary> Synchronizes the item / plushy between host and clients. </summary>
    public static void Sync(ItemIdentifier itemId)
    {
        // the game destroys the itemId of its preview
        if (LobbyController.Lobby == null || itemId == null || itemId.gameObject == null) return;

        // the item was created remotely or loaded from assets
        if (itemId.gameObject.name == "Net" || itemId.gameObject.name.Contains("Book") || itemId.gameObject.scene.name == null) return;

        if (LobbyController.IsOwner)
        {
            var item = itemId.gameObject.AddComponent<Item>();
            Networking.Entities[item.Id] = item;
        }
        else Object.Destroy(itemId.gameObject);
    }

    #endregion
}
