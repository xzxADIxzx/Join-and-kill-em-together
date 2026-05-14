namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of any husk type. </summary>
public class Husk : Enemy
{
    Agent agent;
    Float x, y, z, r;
    NavMeshAgent nma;
    ZombieMelee scr1;
    ZombieProjectiles scr2;
    Animator animator;

    int running = Animator.StringToHash("Running");
    int runmult = Animator.StringToHash("RunSpeed");

    public Husk(uint id, EntityType type) : base(id, type) { }

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

    public override void Create() => Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out animator);
        agent.Get(out nma);
        agent.Get(out scr1, true);
        agent.Get(out scr2, true);
    }

    public override void Update(float delta)
    {
        if (Locked) { nma.enabled = false; scr1?.enabled = false; scr2?.enabled = false; return; }

        scr1?.enabled = IsOwner;
        scr2?.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(agent.Rotation.x,  r.GetAngle(delta), agent.Rotation.z );

        nma.enabled = false;

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr1?.Swing     (); scr2?.Swing(); break;
            case 2: scr1?.JumpAttack(); scr2?.Melee(); break;
        }
        if (LastMoving != Moving)
        {
            animator.SetBool(running, LastMoving = Moving);
            animator.SetFloat(runmult, 1f);
        }
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(ZombieMelee), nameof(ZombieMelee.Swing))]
    [Prefix]
    static void Swing(ZombieMelee __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 1;
    }

    [DynamicPatch(typeof(ZombieMelee), nameof(ZombieMelee.JumpAttack))]
    [Prefix]
    static void Jumpy(ZombieMelee __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 2;
    }

    [DynamicPatch(typeof(ZombieMelee), nameof(ZombieMelee.DamageEnd))]
    [Prefix]
    static void Zeros(ZombieMelee __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 0;
    }

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Swing))]
    [Prefix]
    static void Swing(ZombieProjectiles __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 1;
    }

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Melee))]
    [Prefix]
    static void Melee(ZombieProjectiles __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 2;
    }

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.DamageEnd))]
    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.MeleeDamageEnd))]
    [Prefix]
    static void Zeros(ZombieProjectiles __instance)
    {
        if (__instance.TryGetEntity(out Husk h)) h.Attack = 0;
    }

    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.ThrowProjectile))]
    [DynamicPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.ShootProjectile))]
    [Prefix]
    static bool Peace(ZombieProjectiles __instance) => __instance.name[0] == 'L';

    #endregion
}
