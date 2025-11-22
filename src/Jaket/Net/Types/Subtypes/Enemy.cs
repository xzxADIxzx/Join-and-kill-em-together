namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;

/// <summary> Abstract entity of any enemy type. </summary>
public abstract class Enemy : OwnableEntity
{
    /// <summary> Whether the enemy is a boss. </summary>
    public bool IsBoss;

    public Enemy(uint id, EntityType type) : base(id, type) { }

    public void Heal() { } // TODO remake enemies (again)

    public abstract Transform WeakPoint { get; }
}
