namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net.EntityTypes;

/// <summary> List of all enemies in the game and some useful methods. </summary>
public class Enemies
{
    /// <summary> List of all enemies in the game. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Loads all enemies for future use. </summary>
    public static void Load()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // for some INCREDIBLE reasons, SOME players are missing some enemies on first load, so you need to wait for loading to some level
            if (SceneHelper.CurrentScene == "Main Menu" || Prefabs.Count == 33) return;

            // find all enemy prefabs
            var all = Resources.FindObjectsOfTypeAll<EnemyIdentifier>();

            // null check is needed to make sure that the object is on the right scene
            foreach (var enemy in all) if (enemy.gameObject.scene.name == null) Prefabs.Add(enemy.gameObject);

            // sort enemies by name to make sure their order is the same for different clients
            Prefabs.Sort((p1, p2) => p1.name.CompareTo(p2.name));
        };
    }

    #region index

    /// <summary> Finds enemy index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds enemy index by the name of its clone. </summary>
    public static int CopiedIndex(string name)
    {
        // tell me why
        if (name.StartsWith("SwordsMachine")) return (int)EntityType.Swordsmachine;
        if (name == "V2 Green Arm") return (int)EntityType.V2_GreenArm;
        if (name == "Gabriel 2nd") return (int)EntityType.Gabriel_Angry;

        return Index(name.Contains("(") ? name.Substring(0, name.IndexOf("(")).Trim() : name);
    }

    #endregion
    #region instantiation

    /// <summary> Spawns a remote enemy with the given type. </summary>
    public static RemoteEnemy Instantiate(EntityType type)
    {
        var obj = Object.Instantiate(Prefabs[(int)type]);
        obj.name = "Net"; // needed to prevent object looping between client and server

        var enemy = obj.AddComponent<RemoteEnemy>();
        enemy.Type = type;

        // for some reasons, the size of the Cerberus is smaller than necessary
        if (type == EntityType.Cerberus) obj.transform.localScale = new(4f, 4f, 4f);

        return enemy;
    }

    #endregion
}