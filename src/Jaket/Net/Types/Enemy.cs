namespace Jaket.Net.Types;

using System;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary> Representation of the most enemies in the game responsible for synchronizing position and target of the idols. </summary>
public class Enemy : Entity
{
    /// <summary> Null if the enemy is not a boss. </summary>
    private BossHealthBar healthBar;
    /// <summary> Null if the enemy is not a fake ferryman. </summary>
    private FerrymanFake fakeFerryman;

    /// <summary> Whether the enemy is an idol or not. </summary>
    private Idol idol;
    /// <summary> Idol target id in global entity list. Will be equal to the maximum value if there is no target. </summary>
    private ulong targetId, lastTargetId = ulong.MaxValue;
    /// <summary> Enemy subtype. 0 - standard, 1 - Agony or Angry, 2 - Tundra or Rude. </summary>
    private byte subtype;

    /// <summary> Enemy health, position and rotation. </summary>
    private FloatLerp health, x, y, z, rotation;
    /// <summary> Whether the enemy is a boss and should have a health bar. </summary>
    private bool boss, haveSecondPhase;
    /// <summary> Whether the enemy is a fake ferryman. </summary>
    private bool fake;

    private void Awake()
    {
        Init(Enemies.Type);

        health = new();
        x = new(); y = new(); z = new();
        rotation = new();

        healthBar = GetComponent<BossHealthBar>();
        fakeFerryman = GetComponent<FerrymanFake>();

        // multiply health
        if (LobbyController.IsOwner && healthBar != null)
        {
            if (EnemyId.machine) LobbyController.ScaleHealth(ref EnemyId.machine.health);
            else if (EnemyId.spider) LobbyController.ScaleHealth(ref EnemyId.spider.health);
            else if (EnemyId.statue) LobbyController.ScaleHealth(ref EnemyId.statue.health);

            // in the second phase the same object is used as in the anticipatory one
            if (SceneHelper.CurrentScene == "Level 4-4" && EnemyId.enemyType == EnemyType.V2 && TryGetComponent<V2>(out var V2) && !V2.firstPhase)
            {
                Networking.Entities[Id] = this;
                DestroyImmediate(healthBar);
                healthBar = gameObject.AddComponent<BossHealthBar>();
            }
        }

        // prevent bosses from going into the second phase instantly
        health.target = EnemyId.health;

        // run a loop that will update the target id of the idol every second
        if (TryGetComponent(out idol) && LobbyController.IsOwner) InvokeRepeating("UpdateTarget", 0f, 1f);
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

        if (EnemyId.enemyType == EnemyType.Swordsmachine)
        {
            var original = Tools.ObjFind("S - Secret Fight").transform.GetChild(0).GetChild(0).GetChild(subtype == 1 ? 2 : 1);

            transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = original.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            transform.GetChild(0).GetChild(2).GetComponent<Renderer>().material = original.transform.GetChild(0).GetChild(2).GetComponent<Renderer>().material;
        }

        if (EnemyId.enemyType == EnemyType.Sisyphus)
            foreach (var renderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.material.color = subtype == 1 ? new(1f, .25f, .25f) : new(.25f, .5f, 1f);
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        EnemyId.health = health.Get(LastUpdate);
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(0f, rotation.GetAngel(LastUpdate), 0f);

        // this is necessary so that the health of the bosses is the same for all clients
        if (EnemyId.machine != null) EnemyId.machine.health = EnemyId.health;
        else if (EnemyId.spider != null) EnemyId.spider.health = EnemyId.health;
        else if (EnemyId.statue != null) EnemyId.statue.health = EnemyId.health;

        // add a health bar if the enemy is a boss
        if (boss && healthBar == null) healthBar = gameObject.AddComponent<BossHealthBar>();

        // add the fake ferryman component and destroy the original one
        if (fake && fakeFerryman == null)
        {
            fakeFerryman = gameObject.AddComponent<FerrymanFake>();
            Destroy(GetComponent<Ferryman>());

            // replace the animation controller so that the ferryman sits and does not spin
            GetComponent<Animator>().runtimeAnimatorController = Array.Find(Tools.ResFind<RuntimeAnimatorController>(), c => c.name == "FerrymanIntro2");

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

    /// <summary> Turns on the spawn effect and moves the object so that the effect does not appear at the origin. </summary>
    public void SpawnEffect()
    {
        EnemyId.spawnIn = true;
        transform.position = new(x.target, y.target, z.target);
    }

    /// <summary> Returns boss bar layers created based on maximum health and number of phases. </summary>
    public HealthLayer[] Layers() => haveSecondPhase
            ? new HealthLayer[] { new() { health = health.target / 2f }, new() { health = health.target / 2f } }
            : new HealthLayer[] { new() { health = health.target } };

    #region entity

    public override void Write(Writer w)
    {
        w.Float(EnemyId.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);

        w.Bool(healthBar != null); w.Bool(healthBar == null ? false : healthBar.healthLayers.Length > 1);
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

        boss = r.Bool(); haveSecondPhase = r.Bool();
        fake = r.Bool();
        if (idol) targetId = r.Id();
        subtype = r.Byte();
    }

    public override void Kill()
    {
        // spawn shotgun
        if (TryGetComponent<SwordsMachine>(out var sm) && boss) Instantiate(sm.shotgunPickUp, transform.position, transform.rotation);

        // animate V2's death
        if (!LobbyController.IsOwner && TryGetComponent<V2>(out var v2) && v2.intro && TryGetComponent<Machine>(out var machine))
        {
            v2.active = false;
            v2.escapeTarget = Tools.ObjFind("EscapeTarget")?.transform;
            v2.spawnOnDeath = v2.escapeTarget?.Find("RedArmPickup").gameObject;

            machine.GetHurt(gameObject, Vector3.zero, 1000f, 0f);
            Tools.ObjFind("Music - Versus").GetComponent<Crossfade>().StartFade();
        }
        // it looks funny
        else if (!fake) EnemyId.InstaKill();

        // destroy the boss bar, because it looks just awful
        healthBar?.Invoke("DestroyBar", 3f);
        // destroy the component to allow enemies like Malicious Face and Drone to fall
        DestroyImmediate(fake ? gameObject : this);
    }

    #endregion
}
