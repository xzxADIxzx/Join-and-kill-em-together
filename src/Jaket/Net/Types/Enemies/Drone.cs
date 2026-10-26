namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of any drone type. </summary>
public class Drone : Foe
{
    Agent agent;
    global::Drone scr;

    public Drone(uint id, EntityType type) : base(id, type, true) { }

    #region logic

    public override void Rage(bool enraged)
    {
        base.Rage(enraged);
        if (enraged)
            scr.Enrage();
        else
            scr.UnEnrage();
    }

    public override float Rate(LocalPlayer target) => base.Rate(target) + Networking.Entities.Count(e => e.Type == Type && e.Owner == target.Id) * 16000f;

    public override float Rate(RemotePlayer target) => base.Rate(target) + Networking.Entities.Count(e => e.Type == Type && e.Owner == target.Id) * 16000f;

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out scr);
        agent.Rem<KeepInBounds>();
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = false;
        if (Lock(scr)) return;

        base.Update(delta);

        if (LastAttack != Attack) switch (LastAttack = Attack)
        {
            case 1: scr.Dodge      (default); break;
            case 2: scr.PrepShoot         (); break;
            case 3: scr.ShootSecondary    (); break;
            case 4: scr.SpawnDroneInsignia(); break;
        }
    }

    public override void Killed(bool explode)
    {
        if (Version.DEBUG) Log.Debug($"[ENTS] Killed an entity {Id} due to being {(explode ? "exploded" : "damaged")}");

        if (scr && scr.eid)
        {
            if (explode)
                scr.Explode();
            else
                scr.Death();

            scr.eid.Death();
        }

        Hidden = explode || !agent;
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.Dodge), typeof(Vector3))]
    [Prefix]
    static void Dodge(global::Drone __instance) => __instance.Attack<Drone>(1);

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.PrepShoot))]
    [Prefix]
    static void Shoot(global::Drone __instance) => __instance.Attack<Drone>(2);

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.ShootSecondary))]
    [Prefix]
    static void Pince(global::Drone __instance) => __instance.Attack<Drone>(3);

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.SpawnDroneInsignia))]
    [Prefix]
    static void Crest(global::Drone __instance) => __instance.Attack<Drone>(4);

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.Shoot))]
    [Prefix]
    static bool Peace(global::Drone __instance) => __instance.name[0] == 'L';

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.GetHurt))]
    [Prefix]
    static bool Crash(global::Drone __instance) => __instance.crashing == false || __instance.eid.hitter != "network";

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.Explode))]
    [Prefix]
    static void Break(global::Drone __instance)
    {
        if (__instance.HasEntity(out Drone d) && !d.Hidden) d.Kill(1, w => w.Bools(true, true));
    }

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.Enrage))]
    [Prefix]
    static void Enrage(global::Drone __instance)
    {
        if (__instance.HasEntity(out Drone d) && !d.Enraged) d.Enrage(true);
    }

    [DynamicPatch(typeof(global::Drone), nameof(global::Drone.UnEnrage))]
    [Prefix]
    static void Unrage(global::Drone __instance)
    {
        if (__instance.HasEntity(out Drone d) && d.Enraged) d.Enrage(false);
    }

    #endregion
}
