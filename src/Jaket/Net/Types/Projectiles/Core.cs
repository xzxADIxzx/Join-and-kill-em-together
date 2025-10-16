namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the core type. </summary>
public class Core : Entity
{
    Agent agent;
    Float x, y, z;
    Rigidbody rb;
    Grenade grenade;

    public Core(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 17;

    public override void Write(Writer w)
    {
        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out grenade);
        agent.Rem<FloatingPointErrorPreventer>();
        agent.Rem<RemoveOnTime>();
        agent.Rem<DestroyOnCheckpointRestart>();

        rb.isKinematic = !IsOwner;
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.Get(delta), y.Get(delta), z.Get(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);

        if (left >= 1) // normal (environment), super (any beam), ultra (malicious)
        {
            r.Bools(out var harmless, out var big, out var super, out var ultra, out _, out _, out _, out _);
            grenade.Explode(big, harmless, super, ultra ? 2f : 1f, ultra);
        }
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Grenade), "Start")]
    [HarmonyPrefix]
    static void Start(Grenade __instance)
    {
        if (__instance && !__instance.rocket && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    [HarmonyPrefix]
    static bool Break(Grenade __instance, bool harmless, bool big, bool super, bool ultrabooster)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Core c)
        {
            // it's called after death to spawn some nice explosions
            if (c.Hidden) return true;

            c.Kill(1, w => w.Bools(harmless, big, super, ultrabooster));
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Collision))]
    [HarmonyPrefix]
    static bool Damage(Grenade __instance, Collider other) => Entities.Damage.Deal<Core>(__instance, (eid, tid, ally) =>
    {
        if (ally)
        {
            Physics.IgnoreCollision(__instance.GetComponent<Collider>(), other);
            return false;
        }
        else return true;
    }, other);

    #endregion
}
