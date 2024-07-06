namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.World;

/// <summary> Class that provides entities by their type. </summary>
public class Entities
{
    /// <summary> Dictionary of entity types to their providers. </summary>
    public static Dictionary<EntityType, Prov> Providers = new();
    /// <summary> Last used id, next id's are guaranteed to be greater than it. </summary>
    public static uint LastId;

    /// <summary> Loads providers into the dictionary. </summary>
    public static void Load()
    {
        Providers.Add(EntityType.Player, DollAssets.CreateDoll);

        for (var type = EntityType.Filth; type <= EntityType.Puppet; type++)
        {
            var sucks = type;
            Providers.Add(sucks, () => Enemies.Instantiate(sucks));
        }

        Providers.Add(EntityType.Hand, () => World.Hand);
        Providers.Add(EntityType.Leviathan, () => World.Leviathan);
        Providers.Add(EntityType.Minotaur_Chase, () => World.Minotaur);

        for (var type = EntityType.SecuritySystem_Main; type <= EntityType.SecuritySystem_Tower_; type++)
        {
            var sucks = type;
            Providers.Add(sucks, () => World.SecuritySystem[sucks - EntityType.SecuritySystemOffset]);
        }

        Providers.Add(EntityType.Brain, () => World.Brain);

        for (var type = EntityType.AppleBait; type <= EntityType.V1; type++)
        {
            var sucks = type;
            Providers.Add(sucks, () => Items.Instantiate(sucks));
        }

        Providers.Add(EntityType.Coin, () => Bullets.EInstantiate(EntityType.Coin));
        Providers.Add(EntityType.Rocket, () => Bullets.EInstantiate(EntityType.Rocket));
        Providers.Add(EntityType.Ball, () => Bullets.EInstantiate(EntityType.Ball));
    }

    /// <summary> Instantiates the given prefab and marks it with the Net tag. </summary>
    public static GameObject Mark(GameObject prefab)
    {
        // the instance is created on these coordinates so as not to collide with anything after the spawn
        var instance = Tools.Instantiate(prefab, Vector3.zero);

        instance.name = "Net";
        return instance;
    }

    /// <summary> Returns an entity of the given type. </summary>
    public static Entity Get(uint id, EntityType type)
    {
        var entity = Providers[type]();
        if (entity == null) return null;

        entity.Id = id;
        entity.Type = type;

        return entity;
    }

    /// <summary> Returns the next available id, skips ids of all existing entities. </summary>
    public static uint NextId()
    {
        if (LastId < Tools.AccId) LastId = Tools.AccId;

        LastId++;
        while (Networking.Entities.ContainsKey(LastId)) LastId += 8192;

        return LastId;
    }

    /// <summary> Entity provider. </summary>
    public delegate Entity Prov();
}
