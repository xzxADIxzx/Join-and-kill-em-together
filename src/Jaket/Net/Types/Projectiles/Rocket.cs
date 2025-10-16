namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the rocket type. </summary>
public class Rocket : OwnableEntity
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Cache<RemotePlayer> player;
    Rigidbody rb;
    Grenade grenade;
    Collider[] cs;

    /// <summary> Whether the rocket is frozen. </summary>
    private bool frozen;
    /// <summary> Whether the player is riding. </summary>
    private bool riding;

    public Rocket(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 35;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);

            w.Bool(grenade.frozen);
            w.Bool(grenade.playerRiding);
        }
        else
        {
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);

            w.Bool(frozen);
            w.Bool(riding);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);

        frozen = r.Bool();
        riding = r.Bool();
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Prev = posX.Next, posY.Prev = posY.Next, posZ.Prev = posZ.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out grenade);
        agent.Get(out cs);
        agent.Rem<FloatingPointErrorPreventer>();
        agent.Rem<RemoveOnTime>();
        agent.Rem<DestroyOnCheckpointRestart>();

        OnTransfer = () =>
        {
            player = Owner;
            rb.isKinematic = !IsOwner;
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
            agent.Position = new(posX.Get(delta),      posY.Get(delta),      posZ.Get(delta)     );
            agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));
        }
        cs.Each(c => c.enabled = !riding);

        if (grenade.hooked) TakeOwnage();
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
    static bool Ride(Grenade __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Rocket r)
        {
            if (r.riding) return false;

            r.TakeOwnage();
            return true;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.frozen), MethodType.Getter)]
    [HarmonyPostfix]
    static void Stop(Grenade __instance, ref bool __result)
    {
        if (__instance.name != "O") __result = __instance.name == "S";
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Collision))]
    [HarmonyPrefix]
    static bool Damage(Grenade __instance, Collider other) => Entities.Damage.Deal<Rocket>(__instance, (eid, tid, ally) =>
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
