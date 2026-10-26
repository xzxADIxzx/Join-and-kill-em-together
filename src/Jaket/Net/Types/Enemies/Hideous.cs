namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of the hideous type. </summary>
public class Hideous : Foe
{
    Agent agent;
    Mass scr;
    Animator animator;

    public Hideous(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out scr);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        scr.crazyModeHealth = PostHealth * .2f;

        if (IsOwner) Moving = scr.walking;
        if (Lock(scr)) return;

        base.Update(delta);

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.ToBattle       (); break;
            case 2: scr.ToScout        (); break;
            case 3: scr.ReadySpear     (); break;
            case 4: scr.ExplosiveAttack(); break;
            case 5: scr.BattleSlam     (); break;
            case 6: scr.SwingAttack    (); break;
        }
        animator.SetLayerWeight
        (
            1, // this shrimp is not like the others
            scr.walkWeight = Mathf.MoveTowards(scr.walkWeight, Moving ? 1f : 0f, Time.deltaTime * 4f)
        );
    }

    public override void Killed(bool explode)
    {
        if (explode)
        {
            animator.SetBool("Crazy", true);
            Hidden = !agent;
        }
        else base.Killed(explode);
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(Mass), nameof(Mass.ToBattle))]
    [Prefix]
    static void Fight(Mass __instance) => __instance.Attack<Hideous>(1);

    [DynamicPatch(typeof(Mass), nameof(Mass.ToScout))]
    [Prefix]
    static void Scout(Mass __instance) => __instance.Attack<Hideous>(2);

    [DynamicPatch(typeof(Mass), nameof(Mass.ReadySpear))]
    [Prefix]
    static void Spear(Mass __instance) => __instance.Attack<Hideous>(3);

    [DynamicPatch(typeof(Mass), nameof(Mass.ExplosiveAttack))]
    [Prefix]
    static void Shoot(Mass __instance) => __instance.Attack<Hideous>(4);

    [DynamicPatch(typeof(Mass), nameof(Mass.BattleSlam))]
    [Prefix]
    static void Slamy(Mass __instance) => __instance.Attack<Hideous>(5);

    [DynamicPatch(typeof(Mass), nameof(Mass.SwingAttack))]
    [Prefix]
    static void Swing(Mass __instance) => __instance.Attack<Hideous>(6);

    [DynamicPatch(typeof(Mass), nameof(Mass.ShootProjectile))]
    [Prefix]
    static bool Peace(Mass __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(Mass), nameof(Mass.SpearReturned))]
    [Prefix]
    static void Parry(Mass __instance) => __instance.eid.hitter = "spearparry";

    [DynamicPatch(typeof(Mass), nameof(Mass.CrazyReady))]
    [Prefix]
    static void Phase(Mass __instance)
    {
        if (__instance.HasEntity(out Hideous h) && !h.Hidden) h.Kill(1, w => w.Bools(true, true));
    }

    #endregion
}
