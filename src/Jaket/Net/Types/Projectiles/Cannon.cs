namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the cannonball type. </summary>
public class Cannon : Projectile
{
    Agent agent;
    Float x, y, z;
    Cannonball ball;

    public Cannon(uint id, EntityType type) : base(id, type, true, false) { }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out ball);
        agent.Rem<FloatingPointErrorPreventer>();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);

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
    static void Parry(Cannonball __instance)
    {
        if (__instance.TryGetEntity(out Cannon c)) c.TakeOwnage();
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Unlaunch))]
    [HarmonyPrefix]
    static void Throw(Cannonball __instance)
    {
        if (__instance.TryGetEntity(out Cannon c)) c.TakeOwnage();
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Collide))]
    [HarmonyPrefix]
    static bool Damage(Cannonball __instance, Collider other) => Deal<Cannon>(__instance, (eid, tid, ally, _) =>
    {
        if (ally || __instance.hitEnemies.Contains(eid)) return false;

        float damage = __instance.forceMaxSpeed ? __instance.damage : Mathf.Min(__instance.damage, __instance.rb.velocity.magnitude * .15f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, other: other);

    #endregion
}
