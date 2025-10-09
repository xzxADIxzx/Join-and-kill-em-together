namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the rocket type. </summary>
public class Rocket : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Rigidbody rb;
    Grenade grenade;

    /// <summary> Whether the rocket is frozen. </summary>
    private bool frozen;
    /// <summary> Whether the player is riding. </summary>
    private bool riding;

    public Rocket(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 23;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Bool(grenade.frozen);
            w.Bool(grenade.playerRiding);
        }
        else
        {
            w.Floats(x, y, z);
            w.Bool(frozen);
            w.Bool(riding);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        frozen = r.Bool();
        riding = r.Bool();
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

        OnTransfer = () =>
        {
            player = Owner;
            if (rb && !IsOwner) rb.isKinematic = true;
        };

        OnTransfer();
    }

    public override void Update(float delta)
    {
        agent.name = IsOwner ? "O" : frozen ? "S" : "R";

        if (IsOwner) return;

        if (riding)
            player.Value?.Acquire(agent);
        else
        {
            if (agent.Parent != null) agent.Parent = null;
            agent.Position          = new(x.Get(delta),    y.Get(delta),    z.Get(delta)   );
            agent.transform.forward = new(x.Next - x.Prev, y.Next - y.Prev, z.Next - z.Prev);
        }
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);

        if (left >= 1) // harmless (environment), normal (entity), big (any beam), super (midair), ultra (malicious)
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
        if (__instance && __instance.rocket && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    [HarmonyPrefix]
    static bool Break(Grenade __instance, bool harmless, bool big, bool super, bool ultrabooster)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Rocket r)
        {
            // it's called after death to spawn some nice explosions
            if (r.Hidden) return true;

            r.Kill(1, w => w.Bools(harmless, big, super, ultrabooster));
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.PlayerRideStart))]
    [HarmonyPrefix]
    static void Ride(Grenade __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Rocket r) r.TakeOwnage();
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.frozen), MethodType.Getter)]
    [HarmonyPostfix]
    static void Stop(Grenade __instance, ref bool __result)
    {
        __result = __instance.name == "S";
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Collision))]
    [HarmonyPrefix]
    static bool Damage(Grenade __instance, Collider other)
    {
        if (!other.TryGetComponent(out EnemyIdentifier eid)) eid = other.GetComponent<EnemyIdentifierIdentifier>()?.eid;

        if (__instance.TryGetComponent(out Agent a) && a.Patron is Rocket c)
        {
            if (!eid) return c.IsOwner;
            if (c.IsOwner && eid.TryGetComponent(out Agent b))
            {
                if (b.Patron is RemotePlayer p && p.Team.Ally())
                {
                    Physics.IgnoreCollision(__instance.GetComponent<Collider>(), other);
                    return false;
                }
                else return true;
            }
            return false;
        }
        else return true;
    }

    #endregion
}
