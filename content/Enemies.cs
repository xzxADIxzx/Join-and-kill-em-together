namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net.EntityTypes;

/// <summary> List of all enemies in the game and some useful methods. </summary>
public class Enemies
{
    /// <summary> List of all enemies in the game. </summary>
    public static List<EnemyIdentifier> Prefabs = new();

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
            foreach (var enemy in all) if (enemy.gameObject.scene.name == null) Prefabs.Add(enemy);

            // sort enemies by name to make sure their order is the same for different clients
            Prefabs.Sort((p1, p2) => p1.name.CompareTo(p2.name));
        };
    }

    #region index

    /// <summary> Finds enemy index by its class and type. </summary>
    public static int Index(EnemyIdentifier enemyId) => Prefabs.FindIndex(prefab => prefab.enemyClass == enemyId.enemyClass && prefab.enemyType == enemyId.enemyType);

    /// <summary> Finds enemy index by its class and type, but taking into account the fact that some enemies have the same types. </summary>
    public static int CopiedIndex(EnemyIdentifier enemyId)
    {
        // find enemy name without clone ending
        string name = enemyId.gameObject.name;
        name = name.Contains("(") ? name.Substring(0, name.IndexOf("(")).Trim() : name;

        // then there are the necessary crutches, because the developer incorrectly set the types of opponents
        if (name.StartsWith("V2 Green Arm")) return (int)EntityType.V2_GreenArm;
        if (name == "Very Cancerous Rodent") return (int)EntityType.VeryCancerousRodent;
        if (name == "Mandalore") return (int)EntityType.Mandalore;

        if (name == "Flesh Prison 2") return (int)EntityType.FleshPanopticon;
        if (name == "DroneFlesh") return (int)EntityType.FleshPrison_Eye;
        if (name == "DroneSkull Variant") return (int)EntityType.FleshPanopticon_Face;

        // the remaining enemies can be found by their type
        return Index(enemyId);
    }

    #endregion
    #region instantiation

    /// <summary> Spawns a remote enemy with the given type. </summary>
    public static RemoteEnemy Instantiate(EntityType type)
    {
        // Malicious face's enemyId is in a child object
        // https://discord.com/channels/1132614140414935070/1132614140876292190/1146507403102257162
        var obj = type != EntityType.MaliciousFace ?
                Object.Instantiate(Prefabs[(int)type].gameObject) :
                Object.Instantiate(Prefabs[(int)type].transform.parent.gameObject).transform.GetChild(0).gameObject;
        obj.name = "Net"; // needed to prevent object looping between client and server

        var enemy = obj.AddComponent<RemoteEnemy>();
        enemy.Type = type;

        // for some reasons, the size of the Cerberus is smaller than necessary
        if (type == EntityType.Cerberus) obj.transform.localScale = new(4f, 4f, 4f);

        return enemy;
    }

    #endregion
}