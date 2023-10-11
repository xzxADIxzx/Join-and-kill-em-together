namespace Jaket.Net.EntityTypes;

using System;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary> Representation of the most enemies in the game responsible for synchronizing position and target of the idols. </summary>
public class Enemy : Entity
{
    /// <summary> Enemy identifier component. </summary>
    private EnemyIdentifier enemyId;
    /// <summary> Null if the enemy is not a boss. </summary>
    private BossHealthBar healthBar;
    /// <summary> Null if the enemy is not a fake ferryman. </summary>
    private FerrymanFake fakeFerryman;

    /// <summary> Whether the enemy is an idol or not. </summary>
    private Idol idol;
    /// <summary> Idol target id in global entity list. Will be equal to the maximum value if there is no target. </summary>
    private ulong lastTargetId = ulong.MaxValue, targetId;
    /// <summary> Enemy subtype. 0 - standard, 1 - Agony or Angry, 2 - Tundra or Rude. </summary>
    private byte subtype;

    /// <summary> Enemy health, position and rotation. </summary>
    public FloatLerp health, x, y, z, rotation;
    /// <summary> Whether the enemy is a boss and should have a health bar. </summary>
    public bool boss;
    /// <summary> Whether the enemy is a fake ferryman. </summary>
    public bool fake;

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
        fakeFerryman = GetComponent<FerrymanFake>();

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

    private void Start()
    {
        // find the enemy subtype
        subtype = healthBar?.bossName switch
        {
            "SWORDSMACHINE \"AGONY\"" or "INSURRECTIONIST \"ANGRY\"" => 1,
            "SWORDSMACHINE \"TUNDRA\"" or "INSURRECTIONIST \"RUDE\"" => 2,
            _ => subtype
        };

        // apply the enemy subtype if there is one
        if (LobbyController.IsOwner || subtype == 0) return;

        if (enemyId.enemyType == EnemyType.Swordsmachine)
        {
            var original = GameObject.Find("S - Secret Fight").transform.GetChild(0).GetChild(0).GetChild(subtype == 1 ? 2 : 1);

            transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = original.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            transform.GetChild(0).GetChild(2).GetComponent<Renderer>().material = original.transform.GetChild(0).GetChild(2).GetComponent<Renderer>().material;
        }

        if (enemyId.enemyType == EnemyType.Sisyphus)
            foreach (var renderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.material.color = subtype == 1 ? new(1f, .25f, .25f) : new(.25f, .5f, 1f);
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

        // add the fake ferryman component and destroy the original one
        if (fake && fakeFerryman == null)
        {
            fakeFerryman = gameObject.AddComponent<FerrymanFake>();
            Destroy(GetComponent<Ferryman>());

            // replace the animation controller so that the ferryman sits and does not spin
            GetComponent<Animator>().runtimeAnimatorController = Array.Find(Resources.FindObjectsOfTypeAll<RuntimeAnimatorController>(), c => c.name == "FerrymanIntro2");

            // add components that will trigger an animation when the ferryman touches a coin
            var trigger = UI.Object("Coin Trigger", transform);
            trigger.transform.localPosition = new();
            trigger.transform.localScale = new(3f, 3f, 3f);

            UI.Component<CapsuleCollider>(trigger, collider =>
            {
                collider.height = 2f;
                collider.isTrigger = true;
            });

            UI.Component<CoinActivated>(trigger, coin =>
            {
                coin.disableCoin = true;
                coin.events = new UltrakillEvent() { onActivate = new UnityEvent() };
                coin.events.onActivate.AddListener(() => fakeFerryman?.CoinCatch());
            });
        }

        if (lastTargetId != targetId)
        {
            lastTargetId = targetId;
            idol?.ChangeOverrideTarget( // update idol target to match host
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
        if (!fake) enemyId.InstaKill();

        // reduce health to zero because the host destroyed enemy
        health.target = 0f;

        // destroy the boss bar, because it looks just awful
        if (healthBar != null) healthBar.Invoke("DestroyBar", 2f);

        // destroy the component to allow enemies like Malicious Face and Drone to fall
        Destroy(fake ? gameObject : this);
    }

    public override void Write(Writer w)
    {
        w.Float(enemyId.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);

        w.Bool(healthBar != null);
        w.Bool(fakeFerryman != null);
        if (idol) w.Id(targetId);
        w.Byte(subtype);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        rotation.Read(r);

        boss = r.Bool();
        fake = r.Bool();
        if (idol) targetId = r.Id();
        subtype = r.Byte();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
