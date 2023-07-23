namespace Jaket.Net.EntityTypes;

using Steamworks;
using System;
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
        Owner = SteamClient.SteamId.Value;
        Type = (EntityType)Enemies.CopiedIndex(gameObject.name);

        enemyId = GetComponent<EnemyIdentifier>();
        healthBar = GetComponent<BossHealthBar>();

        if ((int)Type == -1) throw new Exception("Enemy index is -1 for name " + gameObject.name);
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

    public override void Damage(Reader r) => enemyId.DeliverDamage(gameObject, r.Vector(), Vector3.zero, r.Float(), r.Bool(), r.Float(), Bullets.networkDamage);
}
