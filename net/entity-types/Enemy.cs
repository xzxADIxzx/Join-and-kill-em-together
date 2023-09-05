namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of the most enemies in the game responsible for synchronizing position and target of the idols. </summary>
public class Enemy : Entity
{
    /// <summary> Enemy identifier component. </summary>
    private EnemyIdentifier enemyId;
    /// <summary> Null if the enemy is not the boss. </summary>
    private BossHealthBar healthBar;

    /// <summary> Enemy health, position and rotation. </summary>
    public FloatLerp health, x, y, z, rotation;
    /// <summary> Whether the enemy is a boss and should have a health bar. </summary>
    public bool boss;

    private void Awake()
    {
        // interpolations
        health = new FloatLerp();
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();

        // other stuff
        enemyId = GetComponent<EnemyIdentifier>();
        healthBar = GetComponent<BossHealthBar>();

        // prevent bosses from going into the second phase instantly
        health.target = enemyId.health;

        if (LobbyController.IsOwner)
        {
            int index = Enemies.CopiedIndex(enemyId);
            if (index == -1)
            {
                Destroy(this);
                return;
            }

            Id = Entities.NextId();
            Type = (EntityType)index;
        }
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        enemyId.health = health.Get(LastUpdate);
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);

        // this is necessary so that the health of the bosses is the same for all clients
        if (enemyId.machine != null) enemyId.machine.health = enemyId.health;
        else if (enemyId.spider != null) enemyId.spider.health = enemyId.health;
        else if (enemyId.statue != null) enemyId.statue.health = enemyId.health;

        // add a health bar if the enemy is a boss
        if (boss && healthBar == null) healthBar = gameObject.AddComponent<BossHealthBar>();
    }

    /// <summary> Kills the enemy to avoid desynchronization. </summary>
    public void Kill()
    {
        // it looks funny
        enemyId.InstaKill();

        // reduce health to zero because the host destroyed enemy
        health.target = 0f;

        // destroy the boss bar, because it looks just awful
        if (healthBar != null) healthBar.Invoke("DestroyBar", 2f);

        // destroy the component to allow enemies like Malicious Face and Drone to fall
        Destroy(this);
    }

    public override void Write(Writer w)
    {
        w.Float(enemyId.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);

        w.Bool(healthBar != null);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        rotation.Read(r);

        boss = r.Bool();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
