namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;
using Jaket.UI.Lib;

/// <summary> Tangible entity of the core type. </summary>
public class Core : Projectile
{
    Agent agent;
    Grenade gr;

    public Core(uint id, EntityType type) : base(id, type, true, true, true) { }

    #region logic

    public override void Paint(Renderer renderer)
    {
        base.Paint(renderer);
        if (renderer is MeshRenderer m) m.material.mainTexture = null;
        if (renderer is SpriteRenderer s) s.sprite = Tex.Flash;
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out gr);
        agent.Rem<FloatingPointErrorPreventer>();
        agent.Rem<DestroyOnCheckpointRestart>();
        agent.Rem<RemoveOnTime>();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));

        if (gr.hooked) TakeOwnage();
    }

    public override void Killed(Reader r, int left)
    {
        base.Killed(r, left);

        if (left >= 1) // normal (environment), super (any beam), ultra (malicious)
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
        if (__instance && !__instance.rocket && !__instance.enemy) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(Grenade), nameof(Grenade.Explode))]
    [Prefix]
    static bool Death(Grenade __instance, bool harmless, bool big, bool super, bool ultrabooster) => Kill<Core>(__instance, e =>
    {
        e.Kill(1, w => w.Bools(harmless, big, super, ultrabooster));
    }, true);

    [DynamicPatch(typeof(Grenade), nameof(Grenade.GrenadeBeam))]
    [Prefix]
    static void Beamy(Grenade __instance) => Kill<Core>(__instance, e =>
    {
        e.Kill();
    });

    [DynamicPatch(typeof(Grenade), nameof(Grenade.Collision), [typeof(Collider), typeof(Vector3)])]
    [Prefix]
    static bool Damage(Grenade __instance) => __instance.name[0] == 'L';

    #endregion
}
