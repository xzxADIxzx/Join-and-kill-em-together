namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of the cerberus type. </summary>
public class Cerberus : Foe
{
    Agent agent;
    NavMeshAgent nma;
    StatueBoss scr;
    Animator animator;

    static readonly int running = Animator.StringToHash("Walking");

    public Cerberus(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Rage(bool enraged)
    {
        base.Rage(enraged);
        if (enraged)
            scr.Enrage();
        else
            scr.UnEnrage();
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out animator);

        if (Scene == "Level 0-5")
        {
            agent.Scale = Vector3.one * 1.2f;
            agent.Get(out EnemyIdentifier i);

            i.overrideFullName = "CERBERUS, GUARDIAN OF HELL";
        }
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = animator.GetBool(running);
        if (Lock(nma, scr)) return;

        base.Update(delta);

        nma.enabled = false;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.Stomp (); break;
            case 2: scr.Tackle(); break;
            case 3: scr.Throw (); break;
        }
        if (LastMoving != Moving) animator.SetBool(running, LastMoving = Moving);
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Stomp))]
    [Prefix]
    static void Stomp(StatueBoss __instance) => __instance.Attack<Cerberus>(1);

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Tackle))]
    [Prefix]
    static void Melee(StatueBoss __instance) => __instance.Attack<Cerberus>(2);

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Throw))]
    [Prefix]
    static void Throw(StatueBoss __instance) => __instance.Attack<Cerberus>(3);

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.OrbSpawn))]
    [Prefix]
    static bool Peace(StatueBoss __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Enrage))]
    [Prefix]
    static void Enrage(StatueBoss __instance)
    {
        if (__instance.HasEntity(out Cerberus c) && !c.Enraged) c.Enrage(true);
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.UnEnrage))]
    [Prefix]
    static void Unrage(StatueBoss __instance)
    {
        if (__instance.HasEntity(out Cerberus c) && c.Enraged) c.Enrage(false);
    }

    #endregion
}
