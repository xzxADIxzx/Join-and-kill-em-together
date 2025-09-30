namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the cannonball type. </summary>
public class Cannon : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Cannonball ball;
    Renderer[] rs;

    public Cannon(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out ball);
        agent.Get(out rs);
        agent.Rem<FloatingPointErrorPreventer>();

        OnTransfer = () =>
        {
            player = Owner;

            if (!IsOwner) rb.isKinematic = false;
            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                if (r is MeshRenderer m)
                    m.material.color = team.Color() * 100f;
                else
                    r.material.color = team.Color();
            });
        };

        OnTransfer();
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
        ball.Break(); // TODO explosion?
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Cannonball), "Start")]
    [HarmonyPrefix]
    static void Start(Cannonball __instance)
    {
        if (__instance) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Break))]
    [HarmonyPrefix]
    static bool Break(Cannonball __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Cannon c)
        {
            // it's called after death to spawn some nice particles
            if (c.Hidden) return true;

            if (c.IsOwner) c.Kill();
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Launch))]
    [HarmonyPrefix]
    static void Parry(Cannonball __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Cannon c) c.TakeOwnage();
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Unlaunch))]
    [HarmonyPrefix]
    static void Throw(Cannonball __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Cannon c) c.TakeOwnage();
    }

    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Collide))]
    [HarmonyPrefix]
    static bool Damage(Cannonball __instance, Collider other)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Cannon c)
        {
            if (c.IsOwner && (other.TryGetComponent(out EnemyIdentifier e) || (e = other.GetComponent<EnemyIdentifierIdentifier>()?.eid)) && e.TryGetComponent(out Agent b))
            {
                if (b.Patron is RemotePlayer p && p.Team.Ally()) return false;

                // TODO Damage class
                return true;
            }
            return false;
        }
        else return true;
    }

    #endregion
}
