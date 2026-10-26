namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of any husk type. </summary>
public class Husk : Foe
{
    Agent agent;
    NavMeshAgent nma;
    ZombieMelee scr1;
    ZombieProjectiles scr2;
    Animator animator;

    static readonly int running = Animator.StringToHash("Running");
    static readonly int runmult = Animator.StringToHash("RunSpeed");

    public Husk(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr1, true);
        agent.Get(out scr2, true);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = animator.GetBool(running);
        if (Lock(nma, scr1, scr2)) return;

        base.Update(delta);

        nma.enabled = false;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr1?.Swing     (); scr2?.Swing(); break;
            case 2: scr1?.JumpAttack(); scr2?.Melee(); break;
        }
        if (LastMoving != Moving)
        {
            animator.SetBool(running, LastMoving = Moving);
            animator.SetFloat(runmult, 1f);
        }
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(ZombieMelee), nameof(ZombieMelee.Swing))]
    [Prefix]
    static void Swing(ZombieMelee __instance) => __instance.Attack<Husk>(1);

    [DynamicPatch(typeof(ZombieMelee), nameof(ZombieMelee.JumpAttack))]
    [Prefix]
    static void Jumpy(ZombieMelee __instance) => __instance.Attack<Husk>(2);

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Swing))]
    [Prefix]
    static void Swing(ZombieProjectiles __instance) => __instance.Attack<Husk>(1);

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Melee))]
    [Prefix]
    static void Melee(ZombieProjectiles __instance) => __instance.Attack<Husk>(2);

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.ThrowProjectile))]
    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.ShootProjectile))]
    [Prefix]
    static bool Peace(ZombieProjectiles __instance) => __instance.name[0] == 'L';

    #endregion
}
