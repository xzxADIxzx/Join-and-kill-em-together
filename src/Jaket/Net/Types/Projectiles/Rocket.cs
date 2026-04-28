namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;
using Jaket.UI.Lib;

/// <summary> Tangible entity of the rocket type. </summary>
public class Rocket : Projectile
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Grenade gr;

    /// <summary> Whether the rocket is frozen. </summary>
    private bool frozen;
    /// <summary> Whether the player is riding. </summary>
    private bool riding;

    public Rocket(uint id, EntityType type) : base(id, type, true, true, true) { }

    #region snapshot

    public override int BufferSize => 35;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);

            w.Bool(gr.frozen);
            w.Bool(gr.playerRiding);
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

    public override string Name => $"{(IsOwner ? 'L' : frozen ? 'F' : 'R')}#Rocket";

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

        agent.Get(out ObjectActivator act, path: "LevelUpEffect"); act.events.onActivate.AddListener(() => OnTransfer());
        agent.Get(out gr);
        agent.Rem<FloatingPointErrorPreventer>();
        agent.Rem<DestroyOnCheckpointRestart>();
        agent.Rem<RemoveOnTime>();
    }

    public override void Update(float delta)
    {
        agent.name = Name;

        if (IsOwner) return;

        if (!riding)
        {
            if (agent.Parent != null) agent.Parent = null;
            agent.Position = new(posX.GetAware(delta), posY.GetAware(delta), posZ.GetAware(delta));
            agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));
        }
        UpdateRocket(riding);

        if (gr.hooked) TakeOwnage();
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (!IsOwner) agent.Position = new(posX.Init, posY.Init, posZ.Init);

        if (left >= 1) // harmless (environment), normal (entity), big (any beam), super (midair), ultra (malicious)
        {
            r.Bools(out var harmless, out var big, out var super, out var ultra, out _, out _, out _, out _);
            gr.Explode(big, harmless, super, ultra ? 2f : 1f, ultra);
        }
        gr.enemy = true; // check coins
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(Grenade), nameof(Grenade.Start))]
    [Prefix]
    static void Start(Grenade __instance)
    {
        if (__instance && __instance.rocket && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(Grenade), nameof(Grenade.Explode))]
    [Prefix]
    static bool Death(Grenade __instance, bool harmless, bool big, bool super, bool ultrabooster) => Kill<Rocket>(__instance, e =>
    {
        e.Kill(1, w => w.Bools(harmless, big, super, ultrabooster));
    }, true);

    [DynamicPatch(typeof(Grenade), nameof(Grenade.GrenadeBeam))]
    [Prefix]
    static void Beamy(Grenade __instance) => Kill<Rocket>(__instance, e =>
    {
        e.Kill();
    });

    [DynamicPatch(typeof(Grenade), nameof(Grenade.PlayerRideStart))]
    [Prefix]
    static bool Ride(Grenade __instance)
    {
        if (__instance.TryGetEntity(out Rocket r))
        {
            // someone else is riding the rocket
            if (r.riding) return false;

            r.TakeOwnage();
            return true;
        }
        else return true;
    }

    [DynamicPatch(typeof(Grenade), nameof(Grenade.frozen), HarmonyLib.MethodType.Getter)]
    [Postfix]
    static void Stop(Grenade __instance, ref bool __result)
    {
        if (__instance.name[0] != 'L') __result = __instance.name[0] == 'F';
    }

    #endregion
}
