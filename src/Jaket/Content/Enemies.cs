namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> List of all enemies in the game and some useful methods. </summary>
public class Enemies
{
    /// <summary> List of prefabs of all enemies. </summary>
    public static List<EnemyIdentifier> Prefabs = new();

    /// <summary> Loads all enemies for future use. </summary>
    public static void Load()
    {
        foreach (var name in GameAssets.Enemies) Prefabs.Add(GameAssets.Enemy(name).GetComponentInChildren<EnemyIdentifier>());
    }

    /// <summary> Finds the entity type by enemy class and type, taking into account the fact that some enemies have the same types. </summary>
    public static EntityType Type(EnemyIdentifier enemyId)
    {
        // find object name without clone ending
        string name = enemyId.gameObject.name;
        name = name.Contains("(") ? name.Substring(0, name.IndexOf("(")).Trim() : name;

        // there are the necessary crutches, because the developer incorrectly set the types of some opponents
        switch (name)
        {
            case "V2 Green Arm Variant": return EntityType.V2_GreenArm;
            case "Very Cancerous Rodent": return EntityType.VeryCancerousRodent;
            case "DroneFlesh": return EntityType.FleshPrison_Eye;
            case "DroneSkull Variant": return EntityType.FleshPanopticon_Face;
            case "Mandalore": return EntityType.Mandalore;
            case "Big Johninator": return EntityType.Johninator;
        }

        // the remaining enemies can be found by their type
        int index = Prefabs.FindIndex(prefab => prefab.enemyClass == enemyId.enemyClass && prefab.enemyType == enemyId.enemyType);
        return index == -1 ? EntityType.None : (EntityType.EnemyOffset + index);
    }
    public static EntityType Type(Entity entity) => entity.EnemyId == null ? EntityType.None : Type(entity.EnemyId);

    /// <summary> Spawns an enemy with the given type. </summary>
    public static Enemy Instantiate(EntityType type)
    {
        // Malicious face's enemyId is in a child object
        // https://discord.com/channels/1132614140414935070/1132614140876292190/1146507403102257162
        var obj = type != EntityType.MaliciousFace ?
                Object.Instantiate(Prefabs[type - EntityType.EnemyOffset].gameObject) :
                Object.Instantiate(Prefabs[type - EntityType.EnemyOffset].transform.parent.gameObject).transform.GetChild(0).gameObject;

        // for some reasons, the size of the Cerberus is smaller than necessary
        if (type == EntityType.Cerberus) obj.transform.localScale = new(4f, 4f, 4f);

        return obj.AddComponent<Enemy>();
    }

    /// <summary> Synchronizes the enemy between host and clients. </summary>
    public static bool Sync(EnemyIdentifier enemyId)
    {
        if (LobbyController.Lobby == null || enemyId.dead) return true;

        // level 0-2 contains several cutscenes that don't need to be removed
        if (SceneHelper.CurrentScene == "Level 0-2" && enemyId.enemyType == EnemyType.Swordsmachine && enemyId.GetComponent<BossHealthBar>() == null) return true;
        // levels 2-4 and 5-4 contain unique bosses that needs to be dealt with separately
        if (SceneHelper.CurrentScene == "Level 2-4" && enemyId.gameObject.name == "MinosArm")
        {
            enemyId.gameObject.AddComponent<Hand>();
            return true;
        }
        if (SceneHelper.CurrentScene == "Level 5-4" && enemyId.enemyType == EnemyType.Leviathan)
        {
            enemyId.gameObject.AddComponent<Leviathan>();
            return true;
        }

        // the enemy was created remotely
        if (enemyId.gameObject.name == "Net")
        {
            if (!LobbyController.IsOwner) enemyId.GetComponent<Enemy>()?.SpawnEffect();
            return true;
        }
        if (LobbyController.IsOwner)
        {
            if (enemyId.GetComponent<Enemy>() == null) enemyId.gameObject.AddComponent<Enemy>(); // sometimes the game copies enemies
            return true;
        }
        else
        {
            // ask host to spawn enemy if it was spawned via sandbox arm
            if (enemyId.GetComponent<Sandbox.SandboxEnemy>() != null)
                Networking.Send(PacketType.SpawnEntity, w =>
                {
                    w.Enum(Enemies.Type(enemyId));
                    w.Vector(enemyId.transform.position);
                }, size: 13);

            // the enemy is no longer needed, so destroy it
            if (enemyId.enemyType == EnemyType.MaliciousFace && enemyId.gameObject.name == "Body")
                Object.DestroyImmediate(enemyId.transform.parent.gameObject); // avoid a huge number of errors in the console
            else
                Object.DestroyImmediate(enemyId.gameObject);

            return false;
        }
    }

    /// <summary> Synchronizes damage dealt to the enemy. </summary>
    public static bool SyncDamage(EnemyIdentifier enemyId, float damage, bool explode, float critDamage, GameObject source)
    {
        if (LobbyController.Lobby == null || enemyId.dead) return true;

        if (source == Bullets.NetDmg) return true; // the damage was received over the network
        if (source == Bullets.Fake) return false; // bullets are only needed for visual purposes and mustn't cause damage

        if (enemyId.TryGetComponent<Entity>(out var entity) && (entity is not RemotePlayer player || !player.Invincible))
            Bullets.SyncDamage(entity.Id, enemyId.hitter, damage, explode, critDamage);

        // the entity was created before the lobby
        return true;
    }

    /// <summary> Synchronizes the death of the enemy. </summary>
    public static void SyncDeath(EnemyIdentifier enemyId)
    {
        if (LobbyController.Lobby == null || enemyId.dead || !LobbyController.IsOwner) return;

        if (enemyId.TryGetComponent<Enemy>(out var enemy))
        {
            Networking.Send(PacketType.KillEntity, w => w.Id(enemy.Id), size: 8);
            Object.Destroy(enemy);
        }
    }
}
