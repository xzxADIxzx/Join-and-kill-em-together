namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;
using ULTRAKILL.Enemy;

/// <summary> Tangible entity of the shell or any projectile type. </summary>
public class Shell : Projectile
{
    Agent agent;
    global::Projectile proj;

    public Shell(uint id, EntityType type) : base(id, type, true, true, false) { }

    #region logic

    public override void Paint(Renderer renderer)
    {
        if (Type != EntityType.Shell) return;

        base.Paint(renderer);
        if (renderer is MeshRenderer m) m.material.mainTexture = null;
        if (renderer is TrailRenderer t) t.startColor = t.startColor with { a = 1f };
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out proj);
        agent.Rem<FloatingPointErrorPreventer>();
        agent.Rem<DestroyOnCheckpointRestart>();
        agent.Rem<RemoveOnTime>();
        agent.Run(MasterKill, 15f);
    }

    public override void Update(float delta)
    {
        proj.enabled = IsOwner;
        base.Update(delta);
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1)
        {
            if (r.Bool())
            {
                proj.boosted = Type == EntityType.Shell;
                proj.explosionEffect = Entities.Vendor.Prefabs[(byte)EntityType.ShotgunExplosion];
            }
            proj.KeepTrail();
            proj.CreateExplosionEffect();
        }
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(global::Projectile), nameof(global::Projectile.Start))]
    [Prefix]
    static void Start(global::Projectile __instance)
    {
        if (__instance) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(global::Projectile), nameof(global::Projectile.OnDestroy))]
    [Prefix]
    static void Break(global::Projectile __instance) => Kill<Shell>(__instance, e =>
    {
        e.Kill();
    }, true);

    [DynamicPatch(typeof(global::Projectile), nameof(global::Projectile.CreateExplosionEffect))]
    [Prefix]
    static bool Death(global::Projectile __instance) => Kill<Shell>(__instance, e =>
    {
        e.Kill(1, w => w.Bool(__instance.parried));
    }, true);

    [DynamicPatch(typeof(Punch), nameof(Punch.ParryProjectile))]
    [Prefix]
    static void Parry(global::Projectile proj) => Kill<Shell>(proj, e =>
    {
        e.TakeOwnage();
    });

    [DynamicPatch(typeof(ProjectileSpread), nameof(ProjectileSpread.Start))]
    [Postfix]
    static void Spread(ProjectileSpread __instance)
    {
        __instance.projectile.name += "(Clone)";
        Entities.Projectiles.Sync(__instance.projectile);
    }

    [DynamicPatch(typeof(global::Projectile), nameof(global::Projectile.Collided))]
    [Prefix]
    static bool Damage(global::Projectile __instance, Collider other) => Deal<Shell>(__instance, (eid, tid, ally, e) =>
    {
        if (__instance.parried ? ally : EnemyIdentifier.CheckHurtException(__instance.safeEnemyType, eid.enemyType, (TargetHandle)null)) return false;

        Entities.Damage.Deal(tid, __instance.damage * __instance.enemyDamageMultiplier / (__instance.friendly || __instance.playerBullet ? 4f : 10f));
        return true;
    }, other: other);

    #endregion
}
