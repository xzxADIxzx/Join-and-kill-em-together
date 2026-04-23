namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
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
        agent.Rem<DestroyOnCheckpointRestart>();
        agent.Run(MasterKill, 15f);
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        nail.punchable = true;

        if (nail.punched)
        {
            TakeOwnage();
            nail.rb.velocity = (Punch.GetParryLookTarget() - agent.Position).normalized * 200f;
        }
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1 && r.Bool())
            Inst(nail.sawBreakEffect, agent.Position);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(global::Nail), nameof(global::Nail.Start))]
    [HarmonyPrefix]
    static void Start(global::Nail __instance)
    {
        if (__instance && __instance.sawblade && !__instance.chainsaw && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(global::Nail), nameof(global::Nail.SawBreak))]
    [HarmonyPrefix]
    static bool Death(global::Nail __instance) => Kill<Sawblade>(__instance, e =>
    {
        if (e.IsOwner) e.Kill(1, w => w.Bool(true));
    });

    [HarmonyPatch(typeof(global::Nail), nameof(global::Nail.MagnetCaught))]
    [HarmonyPrefix]
    static void Catch(global::Nail __instance) => Events.Post(() =>
    {
        if (__instance.TryGetEntity(out Sawblade s)) s.agent.StopAllCoroutines();
    });

    [HarmonyPatch(typeof(global::Nail), nameof(global::Nail.MagnetRelease))]
    [HarmonyPrefix]
    static void Freed(global::Nail __instance) => Events.Post(() =>
    {
        if (__instance.TryGetEntity(out Sawblade s)) s.agent.Run(s.MasterKill, 15f);
    });

    [HarmonyPatch(typeof(global::Nail), nameof(global::Nail.DamageEnemy))]
    [HarmonyPrefix]
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
