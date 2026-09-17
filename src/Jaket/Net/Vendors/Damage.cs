namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Harmony;

using static Entities;

/// <summary> Vendor responsible for explosions and damage. </summary>
public class Damage : Vendor
{
    public override void Load()
    {
        Fill(EntityType.Shockwave, EntityType.HammerParticleHeavy, GameAssets.Explosions);
    }

    public override EntityType Type(GameObject obj) => EntityType.None;

    public override GameObject Make(EntityType type, Vector3 position = default, Transform parent = null) => null;

    public override void Sync(GameObject obj, params bool[] args) { }

    #region dealing

    /// <summary> Distributes the damage over the network. </summary>
    public void Deal(uint tid, float damage) => Networking.Send(PacketType.Damage, 8, w =>
    {
        w.Id(tid);
        w.Float(damage);
    });

    /// <summary> Delivers remote damage from the network. </summary>
    public void Deal(EnemyIdentifier eid, float damage) => Events.Post(() =>
    {
        eid.hitter = "network";
        eid.DeliverDamage(eid.gameObject, default, default, damage, false);
    });

    #endregion
    #region harmony

    [DynamicPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
    [Prefix]
    static void MeleeDmg(EnemyIdentifier __instance, float multiplier)
    {
        if (__instance.dead || multiplier == 0f) return;

        if (GameAssets.Melee.Has(__instance.hitter) && __instance.TryGetComponent(out Entity.Agent a)) Entities.Damage.Deal(a.Patron.Id, multiplier);

        if (Version.DEBUG) Log.Debug($"[ENTS] Damage of {multiplier} units was dealt by {__instance.hitter}");
    }

    #endregion
}
