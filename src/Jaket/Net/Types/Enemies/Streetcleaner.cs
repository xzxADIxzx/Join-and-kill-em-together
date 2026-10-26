namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of the streetcleaner type. </summary>
public class Streetcleaner : Foe
{
    Agent agent;
    NavMeshAgent nma;
    global::Streetcleaner scr;
    Animator animator;

    static readonly int running = Animator.StringToHash("Running");
    static readonly int walking = Animator.StringToHash("Walking");

    public Streetcleaner(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = animator.GetBool(walking);
        if (Lock(nma, scr)) return;

        base.Update(delta);

        nma.enabled = false;
        scr.dodgeCooldown = 0f;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.StartFire  (); break;
            case 2: scr.StopFire   (); break;
            case 3: scr.Dodge      (); break;
            case 4: scr.DeflectShot(); break;
        }
        if (LastMoving != Moving)
        {
            animator.SetBool(running, LastMoving = Moving);
            animator.SetBool(walking, false);
        }
    }

    public override void Killed(bool explode)
    {
        base.Killed(explode);
        if (explode) scr.CanisterExplosion();
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.StartFire))]
    [Prefix]
    static void Start(global::Streetcleaner __instance) => __instance.Attack<Streetcleaner>(1);

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.StopFire))]
    [Prefix]
    static void Stopp(global::Streetcleaner __instance) => __instance.Attack<Streetcleaner>(2);

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.Dodge))]
    [Postfix]
    static void Dodge(global::Streetcleaner __instance) => __instance.Attack<Streetcleaner>(3);

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.DeflectShot))]
    [Prefix]
    static void Parry(global::Streetcleaner __instance) => __instance.Attack<Streetcleaner>(4);

    #endregion
}
