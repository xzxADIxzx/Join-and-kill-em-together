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
    public FloatLerp x, y, z, rotation;

    /// <summary> Whether the enemy is a boss and should he have a health bar. </summary>
    private bool boss;

    /// <summary> Null if the enemy is not the boss. </summary>
    private BossHealthBar healthBar;

    public void Awake()
    {
        // interpolations
        health = new FloatLerp();
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();

        // other stuff
        enemyId = GetComponent<EnemyIdentifier>();
        health.target = enemyId.health;
    }

    public void Kill()
    {
        // it looks funny
        enemyId.InstaKill();

        // reduce health to zero because the host destroyed LocalEnemy
        health.target = 0f;

        // destroy the boss bar, because it looks just awful
        if (healthBar != null) healthBar.Invoke("DestroyBar", 1f);

        // destroy the component to allow enemies like Malicious Face and Drone to fall
        Destroy(this);
    }

    public void Update()
    {
        // health & position
        enemyId.health = health.Get(LastUpdate);
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);

        // this is necessary so that the health of the bosses is the same for all clients
        if (enemyId.machine != null) enemyId.machine.health = enemyId.health;
        else if (enemyId.spider != null) enemyId.spider.health = enemyId.health;
        else if (enemyId.statue != null) enemyId.statue.health = enemyId.health;

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

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
