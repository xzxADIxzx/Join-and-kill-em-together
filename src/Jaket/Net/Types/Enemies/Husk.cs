namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any husk type. </summary>
public class Husk : Enemy
{
    Agent agent;
    Float x, y, z;
    NavMeshAgent nma;
    ZombieMelee filth;

    /// <summary> Type of an attack being used. </summary>
    private byte attack, lastAttack;

    public Husk(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);

        w.Byte(attack);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);

        attack = r.Byte();
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out filth, true);
    }

    public override void Update(float delta)
    {
        nma.enabled    = IsOwner;
        filth?.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));

        if (lastAttack != attack) switch (lastAttack = attack)
        {
            case 1: filth?.Swing();      break;
            case 2: filth?.JumpAttack(); break;
        }
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

    #endregion
}
