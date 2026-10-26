namespace Jaket.Net.Types;

using ULTRAKILL.Cheats;
using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the magnet type. </summary>
public class Magnet : Rotatable
{
    Agent agent;
    TimeBomb bomb;

    /// <summary> Whether the timer is counting down. </summary>
    private bool counting { get => b0; set => b0 = value; }

    public Magnet(uint id, EntityType type) : base(id, type, true, true, false, true) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out bomb);
    }

    public override void Update(float delta)
    {
        if (IsOwner) { counting = !PauseTimedBombs.Paused && !NoWeaponCooldown.NoCooldown; return; }

        base.Update(delta);
        bomb.activated = false;

        if (counting)
        {
            bomb.timer     = Mathf.MoveTowards(bomb.timer,     0f, Time.deltaTime);
            bomb.beeptimer = Mathf.MoveTowards(bomb.beeptimer, 0f, Time.deltaTime);

            if (bomb.beeptimer == 0f)
                bomb.Beep();
        }
        if (bomb.beeper) bomb.beeper.transform.localScale = Vector3.Lerp(bomb.beeper.transform.localScale, Vector3.zero, Time.deltaTime * 5f);
    }

    public override void Killed(Reader r, int left) => Killed(r, left, agent, bits =>
    {
        if (bits[0]) Inst(bomb.explosion, agent.Position);
    });

    #endregion
    #region harmony

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.Start))]
    [Prefix]
    static void Start(Harpoon __instance)
    {
        if (__instance && !__instance.drill) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.OnDestroy))]
    [Prefix]
    static bool Death(Harpoon __instance) => Kill<Magnet>(__instance, e => e.Kill(1, w => w.Bools(true)));

    [DynamicPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerEnter))]
    [DynamicPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerExit))]
    [Prefix]
    static bool Laggy(Collider other) => other.attachedRigidbody?.name[0] != 'R';

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.OnTriggerEnter))]
    [Prefix]
    static bool Damage(Harpoon __instance, Collider other) => Deal<Magnet>(__instance, (eid, tid, ally, e) => true, other: other);

    #endregion
}
