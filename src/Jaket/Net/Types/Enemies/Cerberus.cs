namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the cerberus type. </summary>
public class Cerberus : Enemy
{
    Agent agent;
    Float x, y, z, r;
    NavMeshAgent nma;
    StatueBoss scr;
    Animator animator;

    static readonly int running = Animator.StringToHash("Walking");

    public Cerberus(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 22;

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
            w.Floats(r);

            w.Byte(Attack);
            w.Bool(Moving);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref this.r);

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

    public override void Create() => Create(Entities.Enemies, ref x, ref y, ref z);

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
        if (Locked) { nma.enabled = false; scr.enabled = false; return; }

        scr.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(agent.Rotation.x,  r.GetAngle(delta), agent.Rotation.z );

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
    static void Stomp(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c)) c.Attack = 1;
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Tackle))]
    [Prefix]
    static void Melee(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c)) c.Attack = 2;
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Throw))]
    [Prefix]
    static void Throw(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c)) c.Attack = 3;
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.StopAction))]
    [Prefix]
    static void Zeros(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c)) c.Attack = 0;
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.OrbSpawn))]
    [Prefix]
    static bool Peace(StatueBoss __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.Enrage))]
    [Prefix]
    static void Enrage(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c) && !c.Enraged) c.Enrage(true);
    }

    [DynamicPatch(typeof(StatueBoss), nameof(StatueBoss.UnEnrage))]
    [Prefix]
    static void Unrage(StatueBoss __instance)
    {
        if (__instance.TryGetEntity(out Cerberus c) && c.Enraged) c.Enrage(false);
    }

    #endregion
}
