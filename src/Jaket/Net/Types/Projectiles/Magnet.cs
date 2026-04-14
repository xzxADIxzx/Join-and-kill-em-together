namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the magnet type. </summary>
public class Magnet : OwnableEntity
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;

    public Magnet(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 29;

    public override void Write(Writer w)
    {
        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);
        }
        else
        {
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);
        }
    }

    public override void Read(Reader r)
    {
        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Init, posY.Init, posZ.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out Rigidbody rb);
        agent.Get(out TrailRenderer trail);

        rb.isKinematic   = !IsOwner;
        trail.startColor = (Networking.Entities[Owner] is RemotePlayer p ? p.Team : Networking.LocalPlayer.Team).Color() with { a = .4f };
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(posX.GetAware(delta), posY.GetAware(delta), posZ.GetAware(delta));
        agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        if (agent) Dest(agent.gameObject);
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
    static void Death(Harpoon __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Magnet m && !m.Hidden) m.Kill();
    }

    [HarmonyPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerEnter))]
    [HarmonyPatch(typeof(global::Magnet), nameof(global::Magnet.OnTriggerExit))]
    [HarmonyPrefix]
    static bool Laggy(Collider other) => other.attachedRigidbody?.name[0] != 'R';

    #endregion
}
