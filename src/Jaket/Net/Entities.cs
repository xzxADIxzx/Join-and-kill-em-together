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
    public static Dictionary<EntityType, Prov> Providers = new(); // TODO replace with array
    /// <summary> Last used id, next id's are guaranteed to be greater than it. </summary>
    public static uint LastId;

    /// <summary> Loads providers into the dictionary. </summary>
    public static void Load()
    {
        Providers.Add(EntityType.Player, ModAssets.CreateDoll);

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
            Providers.Add(sucks, () => World.SecuritySystem[sucks - EntityType.SecuritySystem_Main]);
        }

        Providers.Add(EntityType.Brain, () => World.Brain);

        for (var type = EntityType.BlueSkull; type <= EntityType.Sowler; type++)
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
        var instance = Inst(prefab, Vector3.zero);

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
        if (LastId < AccId) LastId = AccId;

        LastId++;
        while (Networking.Entities.Contains(LastId)) LastId += 8192;

        return LastId;
    }

    /// <summary> Entity provider. </summary>
    public delegate Entity Prov();

    /// <summary> Vendors are responsible for their respective group of entity types. </summary>
    public interface Vendor
    {
        /// <summary> Loads prefabs of entities managed by the vendor. </summary>
        public abstract void Load();

        /// <summary> Resolves the type of the given object or returns none. </summary>
        public abstract EntityType Type(GameObject obj);

        /// <summary> Instantiates an object to manipulate via entity agent. </summary>
        public abstract GameObject Make(EntityType type, Vector3 position = new(), Transform parent = null);

        /// <summary> Synchronizes the given object between network members. </summary>
        public abstract void Sync(GameObject obj, params bool[] args);
    }
}
