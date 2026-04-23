namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the cannonball type. </summary>
public class Cannon : Projectile
{
    Agent agent;
    Cannonball ball;

    public Cannon(uint id, EntityType type) : base(id, type, true, false, false) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out ball);
        agent.Rem<FloatingPointErrorPreventer>();
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1 && r.Bool())
            Inst(ball.breakEffect,           IsOwner ? agent.Position : new(x.Init, y.Init, z.Init));

        if (left >= 2 && r.Bool())
            Inst(ball.interruptionExplosion, IsOwner ? agent.Position : new(x.Init, y.Init, z.Init));
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Start))]
    [HarmonyPrefix]
    static void Start(Cannonball __instance)
    {
        if (__instance && __instance.physicsCannonball) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Break))]
    [HarmonyPrefix]
    static bool Break(Cannonball __instance) => Kill<Cannon>(__instance, e =>
    {
        if (e.IsOwner) e.Kill(1, w => w.Bool(true));
    });

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Explode))]
    [HarmonyPrefix]
    static bool Death(Cannonball __instance) => Kill<Cannon>(__instance, e =>
    {
        e.Kill(2, w => { w.Bool(true); w.Bool(true); });
    });

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Launch))]
    [HarmonyPrefix]
    static void Parry(Cannonball __instance) => Kill<Cannon>(__instance, e =>
    {
        e.TakeOwnage();
    });

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Unlaunch))]
    [HarmonyPrefix]
    static void Throw(Cannonball __instance) => Kill<Cannon>(__instance, e =>
    {
        e.TakeOwnage();
    });

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Collide))]
    [HarmonyPrefix]
    static bool Damage(Cannonball __instance, Collider other) => Deal<Cannon>(__instance, (eid, tid, ally, e) =>
    {
        if (ally || __instance.hitEnemies.Contains(eid)) return false;

        float damage = __instance.forceMaxSpeed ? __instance.damage : Mathf.Min(__instance.damage, __instance.rb.velocity.magnitude * .15f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, other: other);

    #endregion
}
