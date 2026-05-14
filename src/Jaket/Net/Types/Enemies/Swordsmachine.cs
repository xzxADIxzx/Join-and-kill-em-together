namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of any husk type. </summary>
public class Swordsmachine : Enemy
{
    Agent agent;
    Float x, y, z, r;
    NavMeshAgent nma;
    SwordsMachine scr;
    Animator animator;

    int running = Animator.StringToHash("Running");

    public Swordsmachine(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 27;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Float(agent.Rotation.y);

            w.Byte(Attack);
            w.Bool(animator.GetBool(running));
        }
        else
        {
            w.Floats(x, y, z);
            w.Float(r.Next);

            w.Byte(Attack);
            w.Bool(Moving);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        this.r.Set(r.Float());

        Attack = r.Byte();
        Moving = r.Bool();
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

    public override void Create() => Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        if (Locked) { nma.enabled = false; scr.enabled = false; return; }

        scr.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(agent.Rotation.x,  r.GetAngle(delta), agent.Rotation.z );

        nma.enabled = false;

        scr.targetHandle = null;
        scr.phaseChangeHealth = scr.firstPhase ? PostHealth / 2f : 0f;

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
            scr.firstPhase = false;
            scr.EndFirstPhase();
            Hidden = false;
        }
        base.Killed(explode);
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.Combo))]
    [Prefix]
    static void Combo(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s)) s.Attack = 1;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.RunningSwing))]
    [Prefix]
    static void Swing(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s)) s.Attack = 2;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.SwordThrow))]
    [Prefix]
    static void Throw(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s)) s.Attack = 3;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.SwordSpiral))]
    [Prefix]
    static void Twist(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s)) s.Attack = 4;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.DamageStop))]
    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.SwordCatch))]
    [Prefix]
    static void Zeros(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s)) s.Attack = 0;
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.EndFirstPhase))]
    [Prefix]
    static void Phase(EnemyIdentifier __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s) && !s.Hidden) s.Kill(2, w => { w.Bool(true); w.Bool(true); });
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(SwordsMachine.Enrage))]
    [Prefix]
    static void Enrage(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s) && !s.Enraged) s.Enrage(true);
    }

    [DynamicPatch(typeof(SwordsMachine), nameof(MaliciousFace.UnEnrage))]
    [Prefix]
    static void Unrage(SwordsMachine __instance)
    {
        if (__instance.TryGetEntity(out Swordsmachine s) && s.Enraged) s.Enrage(false);
    }

    #endregion
}
