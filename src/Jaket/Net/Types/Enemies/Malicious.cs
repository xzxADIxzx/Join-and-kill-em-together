namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the malicious type. </summary>
public class Malicious : Enemy
{
    Agent agent;
    Float x, y, z, i, j;
    NavMeshAgent nma;
    MaliciousFace scr;
    Transform pr;

    public Malicious(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 25;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Point(pr.eulerAngles);

            w.Byte(Attack);
        }
        else
        {
            w.Floats(x, y, z);
            w.Floats(i, j);

            w.Byte(Attack);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref i, ref j);

        Attack = r.Byte();
    }

    #endregion
    #region logic

    public override void Rage(bool enraged)
    {
        base.Rage(enraged);
        if (enraged)
            scr.Enrage();
        else
            scr.UnEnrage();
    }

    public override float Rate(LocalPlayer target) => nm.hp * nm.hp * 2f + base.Rate(target);

    public override float Rate(RemotePlayer target) => target.Health * target.Health * 2f + base.Rate(target);

    public override void Create() => Create(Entities.Enemies, ref x, ref y, ref z);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out pr, path: "MaliciousFace");
    }

    public override void Update(float delta)
    {
        if (Lock(nma, scr)) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        pr.eulerAngles = new(i.GetAngle(delta), j.GetAngle(delta), 0f               );

        nma.enabled = false;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.beamsAmount = scr.isEnraged ? 2 : 1; scr.ChargeBeam(default); break;
        }
        if (scr.currentCE) scr.BeamChargeUpdate();
    }

    public override void Killed(bool explode)
    {
        if (Version.DEBUG) Log.Debug($"[ENTS] Killed an entity {Id} due to being {(explode ? "exploded" : "damaged")}");

        if (scr && scr.eid)
        {
            if (explode)
            {
                Inst(scr.breakParticle, scr.transform.position, scr.transform.rotation);
                Dest(scr.gameObject);
            }
            else scr.eid.InstaKill();
        }

        LastHidden = explode ? Time.time : float.MaxValue;
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.ChargeBeam))]
    [Prefix]
    static void Beamy(MaliciousFace __instance)
    {
        if (__instance.HasEntity(out Malicious m)) m.Attack = 1;
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.StopWaiting))]
    [Prefix]
    static void Zeros(MaliciousFace __instance)
    {
        if (__instance.HasEntity(out Malicious m)) m.Attack = 0;
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.BeamFire))]
    [Prefix]
    static void Peace(MaliciousFace __instance) => __instance.isBeamPortalBlocked |= __instance.name[0] == 'R';

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.ShootProj))]
    [Prefix]
    static bool Peaoe(MaliciousFace __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.BreakCorpse))]
    [Prefix]
    static void Break(MaliciousFace __instance)
    {
        if (__instance.HasEntity(out Malicious m)) m.Kill(1, w => w.Bools(true, true));
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.Enrage))]
    [Prefix]
    static void Enrage(MaliciousFace __instance)
    {
        if (__instance.HasEntity(out Malicious m) && !m.Enraged) m.Enrage(true);
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.UnEnrage))]
    [Prefix]
    static void Unrage(MaliciousFace __instance)
    {
        if (__instance.HasEntity(out Malicious m) && m.Enraged) m.Enrage(false);
    }

    #endregion
}
