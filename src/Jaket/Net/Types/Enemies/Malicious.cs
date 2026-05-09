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
    Float x, y, z, p, r;
    NavMeshAgent nma;
    MaliciousFace scr;
    Transform pr;

    public Malicious(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 30;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);

            w.Float(pr.eulerAngles.x);
            w.Float(pr.eulerAngles.y);

            w.Byte(Attack);
        }
        else
        {
            w.Floats(x, y, z);

            w.Float(p.Next);
            w.Float(r.Next);

            w.Byte(Attack);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);

        this.p.Set(r.Float());
        this.r.Set(r.Float());

        Attack = r.Byte();
    }

    #endregion
    #region logic

    public override bool Remain => agent;

    public override void Rage(bool enraged)
    {
        base.Rage(enraged);
        if (enraged)
            scr.Enrage();
        else
            scr.UnEnrage();
    }

    public override float Rate(LocalPlayer target) => NewMovement.Instance.hp * NewMovement.Instance.hp * 2f + base.Rate(target);

    public override float Rate(RemotePlayer target) => target.Health * target.Health * 2f + base.Rate(target);

    public override void Create() => Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out pr, path: "MaliciousFace");
    }

    public override void Update(float delta)
    {
        if (Locked) { nma.enabled = false; scr.enabled = false; return; }

        scr.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        pr.eulerAngles = new(p.GetAngle(delta), r.GetAngle(delta), 0f               );

        nma.enabled = false;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.beamsAmount = scr.isEnraged ? 2 : 1; scr.ChargeBeam(default); break;
        }
        if (scr.currentCE) scr.BeamChargeUpdate();
    }

    public override void Killed(bool explode)
    {
        base.Killed(explode);
        if (explode)
        {
            Inst(scr.breakParticle, scr.transform.position, scr.transform.rotation);
            Dest(scr.gameObject);
        }
        scr.spiderCorpseBroken = true; // skip original
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.ChargeBeam))]
    [Prefix]
    static void Beamy(MaliciousFace __instance)
    {
        if (__instance.TryGetEntity(out Malicious m)) m.Attack = 1;
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.StopWaiting))]
    [Prefix]
    static void Zeros(MaliciousFace __instance)
    {
        if (__instance.TryGetEntity(out Malicious m)) m.Attack = 0;
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.BeamFire))]
    [Prefix]
    static void Peace(MaliciousFace __instance) => __instance.isBeamPortalBlocked |= __instance.name[0] == 'R';

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.ShootProj))]
    [Prefix]
    static bool Peaoe(MaliciousFace __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.Enrage))]
    [Prefix]
    static void Siege(MaliciousFace __instance)
    {
        if (__instance.TryGetEntity(out Malicious m) && !m.Enraged) m.Enrage();
    }

    [DynamicPatch(typeof(MaliciousFace), nameof(MaliciousFace.BreakCorpse))]
    [Prefix]
    static void Break(MaliciousFace __instance) => Networking.Entities.Each
    (
        e => e is Malicious m && m.scr == __instance,
        e => e.Kill(2, w => { w.Bool(true); w.Bool(true); })
    );

    #endregion
}
