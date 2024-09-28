namespace Jaket.Net.Types;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Plug designed to prevent respawn of entities. </summary>
public class DeadEntity : Entity
{
    public static DeadEntity Instance;

    /// <summary> List of dead entities used to destroy their corpses in order to optimize the game. </summary>
    public static List<GameObject> Corpses = new();

    public static void Replace(Entity entity)
    {
        Networking.Entities[entity.Id] = Instance;
        Instance.LastUpdate = Time.time;

        Corpses.Add(entity.Type == EntityType.MaliciousFace || entity.Type == EntityType.Cerberus
            ? entity.transform.parent.gameObject
            : entity.gameObject);
    }

    private void Awake()
    {
        Instance = this;
        Dead = true;
    }

    public override void Read(Reader r) { }
    public override void Write(Writer w) { }

    public override void Damage(Reader r) { }
    public override void Kill(Reader r) { }
}
