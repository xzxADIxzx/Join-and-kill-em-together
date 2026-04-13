namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any husk type. </summary>
public class Husk : Enemy
{
    Agent agent;
    Float x, y, z;
    Animator animator;
    NavMeshAgent nma;
    ZombieMelee scr1;
    ZombieProjectiles scr2;

    int running = Animator.StringToHash("Running");

    /// <summary> Type of an attack being used. </summary>
    private byte attack, lastAttack;
    /// <summary> Whether the enemy is running. </summary>
    private bool moving, lastMoving;

    public Husk(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 23;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Byte(attack);
            w.Bool(animator.GetBool(running));
        }
        else
        {
            w.Floats(x, y, z);
            w.Byte(attack);
            w.Bool(moving);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        attack = r.Byte();
        moving = r.Bool();
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
        scr1?.enabled = IsOwner;
        scr2?.enabled = IsOwner;

        if (IsOwner) return;

        nma  .enabled  = false;
        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));

        if (lastAttack != attack) switch (lastAttack = attack)
        {
            case 1: scr1?.Swing();      scr2?.Swing(); break;
            case 2: scr1?.JumpAttack(); scr2?.Melee(); break;
        }
        if (lastMoving != moving) animator.SetBool(running, lastMoving = moving);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(ZombieMelee), nameof(ZombieMelee.Swing))]
    [HarmonyPrefix]
    static void Swing(ZombieMelee __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 1;
    }

    [HarmonyPatch(typeof(ZombieMelee), nameof(ZombieMelee.JumpAttack))]
    [HarmonyPrefix]
    static void Jumpy(ZombieMelee __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 2;
    }

    [HarmonyPatch(typeof(ZombieMelee), nameof(ZombieMelee.DamageEnd))]
    [HarmonyPrefix]
    static void Zeros(ZombieMelee __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 0;
    }

    [HarmonyPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Swing))]
    [HarmonyPrefix]
    static void Swing(ZombieProjectiles __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 1;
    }

    [HarmonyPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.Melee))]
    [HarmonyPrefix]
    static void Melee(ZombieProjectiles __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 2;
    }

    [HarmonyPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.DamageEnd))]
    [HarmonyPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.MeleeDamageEnd))]
    [HarmonyPrefix]
    static void Zeros(ZombieProjectiles __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Husk h) h.attack = 0;
    }

    #endregion
}
