namespace Jaket.Net;

using System.Collections.Generic;

using Jaket.Assets;
using Jaket.Content;
using Jaket.World;

/// <summary> Class that provides entities by their type. </summary>
public class Entities
{
    /// <summary> Dictionary of entity types to their providers. </summary>
    public static Dictionary<EntityType, Prov> Providers = new();
    /// <summary> Last used id, next id's are guaranteed to be greater than it. </summary>
    public static ulong LastId;

    /// <summary> Loads providers into the dictionary. </summary>
    public static void Load()
    {
        for (int i = 0; i <= 32; i++)
        {
            var type = (EntityType)i;
            Providers.Add(type, () => Enemies.Instantiate(type));
        }

        Providers.Add(EntityType.Hand, () => World.Instance.Hand);
        Providers.Add(EntityType.Leviathan, () => World.Instance.Leviathan);

        for (int i = 35; i <= 71; i++)
        {
            var type = (EntityType)i;
            Providers.Add(type, () => Items.Instantiate(type));
        }

        Providers.Add(EntityType.Player, DollAssets.CreateDoll);
    }

    /// <summary> Returns an entity of the given type. </summary>
    public static Entity Get(ulong id, EntityType type)
    {
        var entity = Providers[type]();
        if (entity == null) return null;

        entity.Id = id;
        entity.Type = type;

        return entity;
    }

    /// <summary> Entity provider. </summary>
    public delegate Entity Prov();

    /// <summary> Returns whether the last id has a collision with any player's id. </summary>
    public static bool HasCollisionWithPlayerId()
    {
        foreach (var member in LobbyController.Lobby?.Members)
            if (member.Id == LastId) return true;

        return false;
    }

    /// <summary> Returns the next available id, skips the id of all players. </summary>
    public static ulong NextId()
    {
        do LastId++;
        while (HasCollisionWithPlayerId());

        return LastId;
    }
}