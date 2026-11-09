namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of any sawblade type. </summary>
public class Sawblade : Projectile
{
    Agent agent;
    global::Nail nail;

    public Sawblade(uint id, EntityType type) : base(id, type, true, true, false) { }

    #region logic

    public override void Paint(Renderer renderer)
    {
        base.Paint(renderer);
        if (renderer is MeshRenderer m) m.material.color *= 2f;
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nail);
        agent.Run(MasterKill, 15f);

        if (nail.magnets.Count >= 1) agent.StopAllCoroutines();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        base.Update(delta);
        nail.punchable = true;

        if (nail.punched)
        {
            TakeOwnage();
            nail.rb.velocity = (Punch.GetParryLookTarget() - agent.Position).normalized * 200f;
        }
    }

    public override void Killed(Reader r, int left) => Killed(r, left, agent, bits =>
    {
        if (bits[0]) Inst(nail.sawBreakEffect, agent.Position);
    });

    #endregion
    #region harmony

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.Start))]
    [Prefix]
    static void Start(global::Nail __instance)
    {
        if (__instance && __instance.sawblade && !__instance.chainsaw && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.SawBreak))]
    [Prefix]
    static bool Death(global::Nail __instance) => Kill<Sawblade>(__instance, e => { if (e.IsOwner) e.Kill(1, w => w.Bools(true)); });

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.MagnetCaught))]
    [Postfix]
    static void Catch(global::Nail __instance) => Kill<Sawblade>(__instance, e => { if (__instance.magnets.Count >= 1) e.agent.StopAllCoroutines(); });

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.MagnetRelease))]
    [Postfix]
    static void Freed(global::Nail __instance) => Kill<Sawblade>(__instance, e => { if (__instance.magnets.Count <= 0) e.agent.Run(e.MasterKill, 15f); });

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.DamageEnemy))]
    [Prefix]
    static bool Damage(global::Nail __instance, EnemyIdentifier eid) => Deal<Sawblade>(__instance, (eid, tid, ally, e) =>
    {
        if (ally) { __instance.hitAmount += 1f; return false; }

        float fodder = __instance.GetFodderDamageMultiplier(eid.enemyType);
        float damage = __instance.damage * (__instance.punched ? 2f : 1f) * (__instance.fodderDamageBoost ? fodder : 1f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, eid: eid);

    #endregion
}
