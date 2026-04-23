namespace Jaket.Net.Types;

using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the magnet type. </summary>
public class Magnet : Projectile
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    TimeBomb bomb;

    /// <summary> Whether the timer is counting down. </summary>
    private bool counting;

    public Magnet(uint id, EntityType type) : base(id, type, true, true, false) { }

    #region snapshot

    public override int BufferSize => 34;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);

            w.Bool(!PauseTimedBombs.Paused && !NoWeaponCooldown.NoCooldown);
        }
        else
        {
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);

            w.Bool(counting);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);

        counting = r.Bool();
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Init, posY.Init, posZ.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out bomb);
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(posX.GetAware(delta), posY.GetAware(delta), posZ.GetAware(delta));
        agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));
        bomb.activated = false;

        if (counting)
        {
            bomb.timer     = Mathf.MoveTowards(bomb.timer,     0f, Time.deltaTime);
            bomb.beeptimer = Mathf.MoveTowards(bomb.beeptimer, 0f, Time.deltaTime);

            if (bomb.beeptimer == 0f)
                bomb.Beep();
        }
        if (bomb.beeper) bomb.beeper.transform.localScale = Vector3.Lerp(bomb.beeper.transform.localScale, Vector3.zero, Time.deltaTime * 5f);
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1 && r.Bool())
            Inst(bomb.explosion, agent.Position);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Harpoon), nameof(Harpoon.Start))]
    [HarmonyPrefix]
    static void Start(Harpoon __instance)
    {
        if (__instance && !__instance.drill) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Harpoon), nameof(Harpoon.OnDestroy))]
    [HarmonyPrefix]
    static bool Death(Harpoon __instance) => Kill<Magnet>(__instance, e =>
    {
        if (!e.Hidden) e.Kill(1, w => w.Bool(true));
    });

    [HarmonyPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerEnter))]
    [HarmonyPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerExit))]
    [HarmonyPrefix]
    static bool Laggy(Collider other) => other.attachedRigidbody?.name[0] != 'R';

    [HarmonyPatch(typeof(Harpoon), nameof(Harpoon.OnTriggerEnter))]
    [HarmonyPrefix]
    static bool Damage(Harpoon __instance, Collider other) => Deal<Magnet>(__instance, (eid, tid, ally, e) => true, other: other);

    #endregion
}
