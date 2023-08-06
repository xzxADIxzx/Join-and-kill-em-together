namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary>
/// Local enemy that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote enemy.
/// </summary>
public class LocalEnemy : Entity
{
    /// <summary> Enemy identifier. </summary>
    private EnemyIdentifier enemyId;

    /// <summary> Null if the enemy is not the boss. </summary>
    private BossHealthBar healthBar;

    public void Awake()
    {
        int index = Enemies.CopiedIndex(gameObject.name);
        if (index == -1)
        {
            Destroy(this);
            Debug.LogWarning("Enemy index is -1 for name " + gameObject.name);

            return;
        }

        Id = Entities.NextId();
        Type = (EntityType)index;

        enemyId = GetComponent<EnemyIdentifier>();
        healthBar = GetComponent<BossHealthBar>();
    }

    public override void Write(Writer w)
    {
        // health & position
        w.Float(enemyId.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);

        // boss
        w.Bool(healthBar != null);
    }

    // there is no point in reading anything, because it is a local enemy
    public override void Read(Reader r) => r.Bytes(21); // skip all data

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
