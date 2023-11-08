namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Net.EntityTypes;
using Jaket.World;

/// <summary> List of all items in the game and some useful methods. </summary>
public class Items
{
    /// <summary> List of all items in the game. </summary>
    public static List<Transform> Prefabs = new();

    /// <summary> Loads all items for future use. </summary>
    public static void Load()
    {
        // find and synchronize all the plushies on the level
        Events.OnLoaded += () => Events.Post(() =>
        {
            foreach (var item in Object.FindObjectsOfType<ItemIdentifier>()) SyncPlushy(item);
        });

        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushy(name).transform);
    }

    #region index

    /// <summary> Finds plushy index by its children. </summary>
    public static int PlushyIndex(Transform plushy) => plushy.childCount == 0
        ? -1
        : Prefabs.FindIndex(prefab => prefab.GetChild(prefab.childCount - 1).name == plushy.GetChild(plushy.childCount - 1).name);

    #endregion
    #region instantiation

    /// <summary> Spawns a plushy with the given type. </summary>
    public static Item InstantiatePlushy(EntityType type) => Object.Instantiate(Prefabs[(int)type - 35].gameObject).AddComponent<Item>();

    #endregion
    #region sync

    /// <summary> Synchronizes the plushy between host and clients. </summary>
    public static void SyncPlushy(ItemIdentifier itemId)
    {
        // the game destroys the itemId of its preview
        if (LobbyController.Lobby == null || itemId == null) return;

        // the item was created remotely or loaded from assets
        if (itemId.gameObject.name == "Net" || itemId.gameObject.scene.name == null) return;

        if (LobbyController.IsOwner)
        {
            var item = itemId.gameObject.AddComponent<Item>();
            Networking.Entities[item.Id] = item;
        }
        else Object.Destroy(itemId.gameObject);
    }

    #endregion
}
