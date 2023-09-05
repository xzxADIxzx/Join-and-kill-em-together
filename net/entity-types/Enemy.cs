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

    /// <summary> Whether the enemy is an idol or not. </summary>
    private Idol idol;
    /// <summary> Idol target id in global entity list. Will be equal to the maximum value if there is no target. </summary>
    private ulong lastTargetId = ulong.MaxValue, targetId;

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

        // run a loop that will update the target id of the idol every second
        if (TryGetComponent<Idol>(out idol) && LobbyController.IsOwner) InvokeRepeating("UpdateTarget", 0f, 1f);
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

        if (lastTargetId != targetId)
        {
            lastTargetId = targetId;
            idol.ChangeOverrideTarget( // update idol target to match host
                    Networking.Entities.TryGetValue(targetId, out var entity) && entity != null &&
                    entity.TryGetComponent<EnemyIdentifier>(out var enemy) ? enemy : null);
        }
    }

    /// <summary> Updates the target id of the idol for transmission to clients. </summary>
    public void UpdateTarget() => targetId = idol.target != null && idol.target.TryGetComponent<Enemy>(out var target) ? target.Id : ulong.MaxValue;

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
        if (idol) w.Id(targetId);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        rotation.Read(r);

        boss = r.Bool();
        if (idol) targetId = r.Id();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
