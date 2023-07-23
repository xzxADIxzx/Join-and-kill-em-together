namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

public class RemoteEnemy : Entity
{
    /// <summary> Enemy identifier. </summary>
    public EnemyIdentifier enemyId;

    /// <summary> Enemy health. </summary>
    private FloatLerp health;

    /// <summary> Enemy position and rotation. </summary>
    private FloatLerp x, y, z, rotation;

    /// <summary> Whether the enemy is a boss and should he have a health bar. </summary>
    private bool boss;

    /// <summary> Null if the enemy is not the boss. </summary>
    private BossHealthBar healthBar;

    public void Awake()
    {
        enemyId = GetComponent<EnemyIdentifier>();

        health = new FloatLerp();
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();
    }

    public void Update()
    {
        // health & position
        enemyId.health = health.Get(LastUpdate);
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);

        // boss
        if (boss && healthBar == null) healthBar = gameObject.AddComponent<BossHealthBar>();
    }

    public override void Write(Writer w) {}

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        // health & position
        health.Read(r);
        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);

        // boss
        boss = r.Bool();
    }

    public override void Damage(Reader r) => enemyId.DeliverDamage(gameObject, r.Vector(), Vector3.zero, r.Float(), r.Bool(), r.Float(), Bullets.networkDamage);
}
