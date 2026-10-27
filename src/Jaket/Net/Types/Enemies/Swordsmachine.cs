namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of the swordsmachine type. </summary>
public class Swordsmachine : Foe
{
    Agent agent;
    NavMeshAgent nma;
    SwordsMachine scr;
    Animator animator;

    static readonly int running = Animator.StringToHash("Running");

    public Swordsmachine(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Rage(bool enraged)
    {
        base.Rage(enraged);
        if (enraged)
        {
            if (scr.enraged)
                scr.Enrage();
            else
                scr.Knockdown();
        }
        else scr.UnEnrage();
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = animator.GetBool(running);
        if (Lock(nma, scr)) return;

        base.Update(delta);

        nma.enabled = false;
        scr.targetHandle = null;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.Combo       (); break;
            case 2: scr.RunningSwing(); break;
            case 3: scr.SwordThrow  (); break;
            case 4: scr.SwordSpiral (); break;
        }
        if (LastMoving != Moving) animator.SetBool(running, LastMoving = Moving);
    }

    public override void Killed(bool explode)
    {
        if (explode)
        {
            if (Boss) ResFind<WeaponPickUp>().Each
            (
                w => IsReal(w) && (w.transform.position - agent.Position).magnitude < 70f,
                w => scr.shotgunPickUp = w.gameObject
            );
            scr.bossVersion = true;
            scr.firstPhase = false;
            scr.EndFirstPhase();
            Hidden = false;
        }
        else base.Killed(explode);
    }

    public override void Bossed(int layers) { base.Bossed(layers); scr.phaseChangeHealth = PostHealth / 2f; }

    #endregion
    #region harmony

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.Combo))]
    [Prefix]
    static void Combo(SwordsMachine __instance) => __instance.Attack<Swordsmachine>(1);

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.RunningSwing))]
    [Prefix]
    static void Swing(SwordsMachine __instance) => __instance.Attack<Swordsmachine>(2);

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.SwordThrow))]
    [Prefix]
    static void Throw(SwordsMachine __instance) => __instance.Attack<Swordsmachine>(3);

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.SwordSpiral))]
    [Prefix]
    static void Twist(SwordsMachine __instance) => __instance.Attack<Swordsmachine>(4);

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.EndFirstPhase))]
    [Prefix]
    static void Phase(SwordsMachine __instance)
    {
        if (__instance.HasEntity(out Swordsmachine s) && !s.Hidden) s.Kill(1, w => w.Bools(true, true));
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.TeleportAway))]
    [Prefix]
    static bool Leave(SwordsMachine __instance)
    {
        if (__instance.HasEntity(out Swordsmachine s) && !s.Hidden) s.Kill();
        return false;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.Enrage))]
    [Prefix]
    static void Enrage(SwordsMachine __instance)
    {
        if (__instance.HasEntity(out Swordsmachine s) && !s.Enraged) s.Enrage(true);
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.UnEnrage))]
    [Prefix]
    static void Unrage(SwordsMachine __instance)
    {
        if (__instance.HasEntity(out Swordsmachine s) && s.Enraged) s.Enrage(false);
    }

    #endregion
}
