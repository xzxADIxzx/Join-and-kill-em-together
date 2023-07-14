namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Net;

/// <summary> List of all enemies in the game and some useful methods. </summary>
public class Enemies
{
    /// <summary> List of all enemies in the game. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Loads all enemies for future use. </summary>
    public static void Load()
    {
        var all = Resources.FindObjectsOfTypeAll<EnemyIdentifier>();
        foreach (var enemy in all) Prefabs.Add(enemy.gameObject);

        Prefabs.ForEach(prefab => Debug.LogWarning(prefab.name));
    }

    #region index

    /// <summary> Finds enemy index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds enemy index by the name of its clone. </summary>
    public static int CopiedIndex(string name) => Index(name.Substring(0, name.IndexOf("(Clone)")));

    #endregion
    #region instantiation

    /// <summary> Spawns a remote enemy with the given type. </summary>
    public static RemoteEnemy Instantiate(EntityType type)
    {
        var obj = Object.Instantiate(Prefabs[(int)type]);
        obj.name = "Net"; // needed to prevent object looping between client and server

        var enemy = obj.AddComponent<RemoteEnemy>();
        enemy.Type = type;

        return enemy;
    }

    #endregion
}