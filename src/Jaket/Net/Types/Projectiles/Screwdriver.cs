namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the screwdriver type. </summary>
public class Screwdriver : OwnableEntity
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Cache<Entity> target;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Harpoon harp;
    Renderer[] rs;

    public Screwdriver(uint id, EntityType type) : base(id, type) { }

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

        var id = r.Id();
        if (id != target) target = id;
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Prev = posX.Next, posY.Prev = posY.Next, posZ.Prev = posZ.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out harp);
        agent.Get(out rs);

        OnTransfer = () =>
        {
            player = Owner;

            rb.isKinematic = !IsOwner;
            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                    r.material.color = team.Color() * 8f;
            });

            if (!IsOwner)
            {
                harp.CancelInvoke();
                Set("stopped",  harp, false);
                Set("drilling", harp, false);
            }
        };

        Locked = false;

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(posX.Get(delta),      posY.Get(delta),      posZ.Get(delta)     );
        agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));

        // TODO if target is gotten, add harpoon to the list of drillers so that you can punch it
        // TODO depending on target, enable/disable the trail (tr.emitting)
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        if (agent) Dest(agent.gameObject);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Harpoon), "Start")]
    [HarmonyPrefix]
    static void Start(Harpoon __instance)
    {
        if (__instance && __instance.drill) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Harpoon), "OnDestroy")]
    [HarmonyPrefix]
    static void Death(Harpoon __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Screwdriver s) s.Kill();
    }

    [HarmonyPatch(typeof(Harpoon), nameof(Harpoon.Punched))]
    [HarmonyPrefix]
    static void Parry(Harpoon __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Screwdriver s) { s.TakeOwnage(); s.target = 0u; }
    }

    [HarmonyPatch(typeof(Punch), "ActiveEnd")]
    [HarmonyPrefix]
    static void Punch() => Networking.Entities.Alive(e =>
    {
        if (e is Screwdriver s && s.target == AccId)
        {
            Set("aud", s.harp, s.harp.GetComponent<AudioSource>());
            s.harp.transform.forward  = CameraController.Instance.transform.forward;
            s.harp.transform.position = CameraController.Instance.GetDefaultPos();
            s.harp.Punched();
            TimeController.Instance.ParryFlash();
        }
    });

    [HarmonyPatch(typeof(Harpoon), "OnTriggerEnter")]
    [HarmonyPrefix]
    static bool Damage(Harpoon __instance, Collider other, float ___damageLeft) => Entities.Damage.Deal<Screwdriver>(__instance, (eid, tid, ally, screwdriver) =>
    {
        if (ally || eid.drillers.Contains(__instance)) return false;

        screwdriver.target = tid;

        Entities.Damage.Deal(tid, ___damageLeft);
        return true;
    }, other);

    #endregion
}
