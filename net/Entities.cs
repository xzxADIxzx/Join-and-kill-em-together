namespace Jaket.Net;

using System.Collections.Generic;

using Jaket.Content;

/// <summary> Class that provides entities by their type. </summary>
public class Entities
{
    /// <summary> Dictionary of entity types to their providers. </summary>
    public static Dictionary<EntityType, Prov> providers = new Dictionary<EntityType, Prov>();

    /// <summary> Loads providers into the dictionary. </summary>
    public static void Load()
    {
        for (int i = 0; i <= 30; i++)
        {
            var type = (EntityType)i;
            providers.Add(type, () => Enemies.Instantiate(type));
        }
        providers.Add(EntityType.Player, RemotePlayer.CreatePlayer);
    }

    /// <summary> Returns an entity of the given type. </summary>
    public static Entity Get(EntityType type) => providers[type]();

    /// <summary> Entity provider. </summary>
    public delegate Entity Prov();
}