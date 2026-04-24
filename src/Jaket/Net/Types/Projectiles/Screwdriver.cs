namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;
using Jaket.UI.Lib;

/// <summary> Tangible entity of the screwdriver type. </summary>
public class Screwdriver : Projectile
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Cache<Entity> target;
    Harpoon harp;

    public Screwdriver(uint id, EntityType type) : base(id, type, true, true, false) { }

    #region snapshot

    public override int BufferSize => 37;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);

            w.Id(target);
        }
        else
        {
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);

            w.Id(target);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);

        target = r.Id();
    }

    #endregion
    #region logic

    public override void Paint(Renderer renderer)
    {
        base.Paint(renderer);
        if (renderer is MeshRenderer m) m.material.mainTexture = null;
        if (renderer is SpriteRenderer s) s.sprite = Tex.Flash;
    }

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Init, posY.Init, posZ.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out harp);
        agent.Get(out harp.aud);
        agent.Rem<TimeBomb>();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(posX.GetAware(delta), posY.GetAware(delta), posZ.GetAware(delta));
        agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));

        harp.stopped     = false;
        harp.drilling    = false;
        harp.target      = null;
        harp.tr.emitting = target == 0u;

        if (harp.fj               ) Dest(harp.fj               );
        if (harp.currentDrillSound) Dest(harp.currentDrillSound);
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1 && r.Bool())
            Inst(harp.breakEffect, agent.Position);
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.Start))]
    [Prefix]
    static void Start(Harpoon __instance)
    {
        if (__instance && __instance.drill) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.DestroyIfNotHit))]
    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.MasterDestroy))]
    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.SlowUpdate))]
    [Prefix]
    static bool Break(Harpoon __instance) => false;

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.OnDestroy))]
    [Prefix]
    static bool Death(Harpoon __instance) => Kill<Screwdriver>(__instance, e =>
    {
        if (!e.Hidden) e.Kill(1, w => w.Bool(true));
    });

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.Punched))]
    [Prefix]
    static void Parry(Harpoon __instance) => Kill<Screwdriver>(__instance, e =>
    {
        e.TakeOwnage();
        e.target = 0u;
    });

    [DynamicPatch(typeof(Punch), nameof(global::Punch.ActiveEnd))]
    [Prefix]
    static void Punch()
    {
        if (FistControl.Instance.currentPunch.type != FistType.Standard) return;

        Networking.Entities.Alive<Screwdriver>(s => s.target == AccId, s =>
        {
            s.harp.transform.forward  = CameraController.Instance.transform.forward;
            s.harp.transform.position = CameraController.Instance.transform.position;
            s.harp.Punched();

            TimeController.Instance.ParryFlash();
            FistControl.Instance.currentPunch.anim.Play("Hook", 0, .065f);
            global::Punch.GetParryLookTarget();
        });
    }

    [DynamicPatch(typeof(Harpoon), nameof(Harpoon.OnTriggerEnter))]
    [Prefix]
    static bool Damage(Harpoon __instance, Collider other) => Deal<Screwdriver>(__instance, (eid, tid, ally, e) =>
    {
        if (ally || __instance.target?.eid == eid) return false;

        e.target = tid;

        Entities.Damage.Deal(tid, __instance.damageLeft);
        return true;
    }, other: other);

    #endregion
}
