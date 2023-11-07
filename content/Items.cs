namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net.EntityTypes;

/// <summary> List of all items in the game and some useful methods. </summary>
public class Items
{
    /// <summary> List of all items in the game. </summary>
    public static List<Transform> Prefabs = new();

    /// <summary> Loads all items for future use. </summary>
    public static void Load()
    {
        foreach (var name in GameAssets.Plushies) Prefabs.Add(GameAssets.Plushy(name).transform);
    }

    #region index

    /// <summary> Finds plushy index by its children. </summary>
    public static int PlushyIndex(Transform plushy) =>
        Prefabs.FindIndex(prefab => prefab.GetChild(prefab.childCount - 1).name == plushy.GetChild(plushy.childCount - 1).name);

    #endregion
    #region instantiation

    /// <summary> Spawns a plushy with the given type. </summary>
    public static Item InstantiatePlushy(EntityType type) => Object.Instantiate(Prefabs[(int)type - 35].gameObject).AddComponent<Item>();

    #endregion
}
