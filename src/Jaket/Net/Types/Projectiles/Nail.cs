namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of any nail type. </summary>
public class Nail : Projectile
{
    Agent agent;
    Rigidbody rb;

    public Nail(uint id, EntityType type) : base(id, type, true, true, true) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out rb);
        agent.Rem<DestroyOnCheckpointRestart>();
        agent.Run(MasterKill, 5f);

        if (!IsOwner) agent.Rem<CapsuleCollider>();
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.Start))]
    [Prefix]
    static void Start(global::Nail __instance)
    {
        if (__instance && !__instance.sawblade && !__instance.chainsaw && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.RemoveTime))]
    [Prefix]
    static bool Break(global::Nail __instance) => false;

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.OnDestroy))]
    [Prefix]
    static void Death(global::Nail __instance) => Kill<Nail>(__instance, e => e.Kill(), true);

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.MagnetCaught))]
    [Prefix]
    static void Catch(global::Nail __instance) => Events.Post(() =>
    {
        if (__instance.TryGetEntity(out Nail n)) n.agent.StopAllCoroutines();
    });

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.MagnetRelease))]
    [Prefix]
    static void Freed(global::Nail __instance) => Events.Post(() =>
    {
        if (__instance.TryGetEntity(out Nail n)) n.agent.Run(n.MasterKill, 5f);
    });

    [DynamicPatch(typeof(Punch), nameof(Punch.BlastCheck))]
    [Prefix]
    static void Blast(Punch __instance)
    {
        if (__instance.heldAction.IsPressed()) Networking.Entities.Alive<Nail>(e => e.rb && (e.agent.Position - NewMovement.Instance.transform.position).sqrMagnitude < 100f, e =>
        {
            e.agent.StopAllCoroutines();
            e.agent.Run(e.MasterKill, 5f);

            e.TakeOwnage();
            e.rb.velocity = CameraController.Instance.transform.forward * 80f;
            e.rb.useGravity = false;

            e.agent.Get(out global::Nail n); n.damage = 2f;
            e.agent.Get(out Collider trggr); trggr.enabled = true;
        });
    }

    [DynamicPatch(typeof(global::Nail), nameof(global::Nail.DamageEnemy))]
    [Prefix]
    static bool Damage(global::Nail __instance, EnemyIdentifier eid) => Deal<Nail>(__instance, (eid, tid, ally, e) =>
    {
        e.agent.StopAllCoroutines();
        e.agent.Run(e.MasterKill, 90f);

        float fodder = __instance.GetFodderDamageMultiplier(eid.enemyType);
        float damage = __instance.damage * (__instance.fodderDamageBoost ? fodder : 1f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, eid: eid);

    #endregion
}
