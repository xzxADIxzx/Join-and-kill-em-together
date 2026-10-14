namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the streetcleaner type. </summary>
public class Streetcleaner : Enemy
{
    Agent agent;
    Float x, y, z, j;
    NavMeshAgent nma;
    global::Streetcleaner scr;
    Animator animator;

    static readonly int running = Animator.StringToHash("Running");
    static readonly int walking = Animator.StringToHash("Walking");

    public Streetcleaner(uint id, EntityType type) : base(id, type) { }

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
            w.Bool(animator.GetBool(walking));
        }
        else
        {
            w.Floats(x, y, z);
            w.Floats(j);

            w.Byte(Attack);
            w.Bool(Moving);
        }

        Attack = 0;
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref j);

        Attack = r.Byte();
        Moving = r.Bool();
    }

    #endregion
    #region logic

    public override void Create() => Create(Entities.Enemies, ref x, ref y, ref z);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out animator);
    }

    public override void Update(float delta)
    {
        if (Lock(nma, scr)) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(agent.Rotation.x,  j.GetAngle(delta), agent.Rotation.z );

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
    static void Start(global::Streetcleaner __instance)
    {
        if (__instance.HasEntity(out Streetcleaner s)) s.Attack = 1;
    }

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.StopFire))]
    [Prefix]
    static void Stopp(global::Streetcleaner __instance)
    {
        if (__instance.HasEntity(out Streetcleaner s)) s.Attack = 2;
    }

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.Dodge))]
    [Postfix]
    static void Dodge(global::Streetcleaner __instance)
    {
        if (__instance.HasEntity(out Streetcleaner s)) s.Attack = 3;
    }

    [DynamicPatch(typeof(global::Streetcleaner), nameof(global::Streetcleaner.DeflectShot))]
    [Prefix]
    static void Parry(global::Streetcleaner __instance)
    {
        if (__instance.HasEntity(out Streetcleaner s)) s.Attack = 4;
    }

    #endregion
}
