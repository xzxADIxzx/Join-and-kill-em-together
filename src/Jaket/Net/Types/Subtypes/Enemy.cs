namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Abstract entity of any enemy type. </summary>
public abstract class Enemy : OwnableEntity
{
    static NewMovement nm => NewMovement.Instance;

    Agent agent;
    Cache<RemotePlayer> player;
    EnemyIdentifier enemyId;
    global::Enemy enemy;
    BossHealthBar bossbar;

    /// <summary> Whether the enemy is a boss. </summary>
    public bool Boss;
    /// <summary> Whether the enemy is madden. </summary>
    public bool Enraged;
    /// <summary> Whether the enemy is idoled. </summary>
    public bool Blessed => enemyId.Blessed;

    /// <summary> Whether the enemy is acting. </summary>
    public byte Attack, LastAttack = byte.MaxValue;
    /// <summary> Whether the enemy is moving. </summary>
    public bool Moving, LastMoving;

    /// <summary> Initial health of the enemy. </summary>
    public float InitHealth;
    /// <summary> PostPPP health of the enemy. </summary>
    public float PostHealth;

    public Enemy(uint id, EntityType type) : base(id, type) { }

    #region logic

    public void Enrage(bool enraged = true) => Kill(4, w =>
    {
        w.Bool(false);
        w.Bool(false);
        w.Bool(true);
        w.Bool(enraged);
    });

    public virtual Transform WeakPoint => enemyId.weakPoint?.transform ?? agent.transform;

    public virtual EnemyTarget Tracked => enemyId.target = IsOwner ? EnemyTarget.TrackPlayerIfAllowed() : player.Value?.Target;

    public virtual bool Remain => false;

    public virtual void Heal() => enemy.health = Mathf.Min(PostHealth, enemy.health + PostHealth / (LobbyController.Lobby?.MemberCount ?? 1f));

    public virtual void Rage(bool enraged) => Enraged = enraged;

    public virtual float Rate(LocalPlayer target) => nm.dead ? float.MaxValue : (nm.transform.position - agent.Position).sqrMagnitude;

    public virtual float Rate(RemotePlayer target) => (target.Position - agent.Position).sqrMagnitude;

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out enemyId);
        agent.Get(out enemy, true);
        agent.Get(out bossbar, true);

        OnTransfer = () =>
        {
            player = Owner;
            _ = Tracked;

            agent.StopAllCoroutines();
            agent.Run(() => _ = Tracked, 1f, true);

            if (IsOwner) agent.Run(() =>
            {
                var rate = Rate(Networking.LocalPlayer);
                var best = Networking.LocalPlayer.Id;

                Networking.Entities.Player(p => p.Health > 0, p =>
                {
                    var test = Rate(p);
                    if (test < rate)
                    {
                        rate = test;
                        best = p.Id;
                    }
                });
                TransferTo = best;
            }, 4f, true);
        };

        Boss = bossbar; // non-owners will read this value to create a bossbar
        Locked = false;

        InitHealth = enemy?.health ?? 0f;
        PostHealth = InitHealth * (1f + LobbyConfig.PPP / 10f * (LobbyController.Lobby?.MemberCount - 1 ?? 0));

        OnTransfer();

        if (Boss) agent.Run(() => Kill(10, w =>
        {
            w.Bool(false);
            w.Bool(true);

            w.Float(PostHealth);
            w.Int(bossbar.healthLayers.Length);
        }), .1f);
    }

    public override void Damage(Reader r) => Entities.Damage.Deal(enemyId, r.Float());

    public override void Killed(Reader r, int left)
    {
        if (left == 0)
        {
            Hidden = true;
            if (agent) Dest(agent.gameObject);
        }

        if (left >= 1 && r.Bool())
        {
            if (Remain)
                LastHidden = Time.time + 240f;
            else
                Hidden = true;

            Killed(r.Bool());
            return;
        }

        if (left >= 2 && r.Bool())
        {
            Boss = true;
            PostHealth = enemy.health = r.Float();

            int layers = r.Int();
            if (layers == 0) return;

            bossbar ??= agent.GetOrAddComponent<BossHealthBar>();
            bossbar.healthLayers = new HealthLayer[layers];

            for (int i = 0; i < layers; i++) bossbar.healthLayers[i] = new() { health = PostHealth / layers };
            BossBarManager.Instance.bossBarsToRemove.Enqueue(bossbar.bossBarId);
        }

        if (left >= 3 && r.Bool()) Rage(r.Bool());
    }

    public virtual void Killed(bool explode)
    {
        if (Version.DEBUG) Log.Debug($"[ENTS] Killed an entity {Id} due to being {(explode ? "exploded" : "damaged")}");

        if (explode)
            enemyId.Explode();
        else
            enemyId.InstaKill();

        Dest(agent);
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Start))]
    [Prefix]
    static void Start(EnemyIdentifier __instance)
    {
        if (__instance) Entities.Enemies.Sync(__instance.gameObject, __instance.IsSandboxEnemy);
    }

    [DynamicPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Explode))]
    [Prefix]
    static void Break(EnemyIdentifier __instance)
    {
        if (__instance.TryGetEntity(out Enemy e) && !e.Hidden) e.Kill(2, w => { w.Bool(true); w.Bool(true); });
    }

    [DynamicPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.ProcessDeath))]
    [Prefix]
    static void Death(EnemyIdentifier __instance)
    {
        if (__instance.TryGetEntity(out Enemy e) && !e.Hidden) e.Kill(2, w => { w.Bool(true); w.Bool(false); });
    }

    [DynamicPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.UpdateTarget))]
    [Prefix]
    static bool Focus() => false;

    [DynamicPatch(typeof(global::Enemy), nameof(global::Enemy.OnTravel))]
    [Prefix]
    static bool Fraud(EnemyScript ___script) => ___script != null;

    #endregion
}
