namespace Jaket.Net;

using System.Collections.Generic;

/// <summary> Class that provides entities by their type. </summary>
public class Entities
{
    /// <summary> Dictionary of entity types to their providers. </summary>
    public static Dictionary<Type, Prov> providers = new Dictionary<Type, Prov>();

    /// <summary> Loads providers into the dictionary. </summary>
    public static void Load()
    {
        providers.Add(Type.player, RemotePlayer.CreatePlayer);
    }

    /// <summary> Returns an entity of the given type. </summary>
    public static Entity Get(Type type)
    {
        providers.TryGetValue(type, out var entity);
        return entity.Invoke();
    }

    /// <summary> Entity provider. </summary>
    public delegate Entity Prov();

    /// <summary> All entity types. Will replenish over time. </summary>
    public enum Type
    {
        player = 0,
        enemy = 1
    }
}