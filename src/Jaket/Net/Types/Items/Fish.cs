namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

/// <summary> Tangible entity of any fish type. </summary>
public class Fish : Item
{
    ItemIdentifier itemId;
    FishObjectReference fish;
    ObjectActivator timer;

    public Fish(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => new(10f, 230f, 110f);

    public override void Assign(Agent agent)
    {
        base.Assign(agent);

        agent.TryGetComponent(out itemId);
        agent.TryGetComponent(out fish);

        FishManager.Instance.UnlockFish(fish.fishObject);
    }

    public override void Update(float delta)
    {
        base.Update(delta);

        if (Type == EntityType.FishBomb && !itemId.pickedUp && !timer) timer = Component<ObjectActivator>(fish.gameObject, a =>
        {
            a.ActivateDelayed(2.4f);
            a.events = new() { onActivate = new() };
            a.events.onActivate.AddListener(() =>
            {
                Killed(default, -1);
                var pos = fish.transform.position;
                GameAssets.Prefab("Attacks and Projectiles/Explosions/Explosion Harmless.prefab", p => Inst(p, pos));
            });
        });
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(FishCooker), "OnTriggerEnter")]
    [HarmonyPrefix]
    static bool Cook(Collider other, bool ___unusable)
    {
        var agent = other.GetComponentInParent<Agent>();
        if (agent && Scene == "Level 5-S")
        {
            if (agent.Patron is Item i && i.IsOwner && i.Type != EntityType.FishCooked && i.Type != EntityType.FishBurnt)
            {
                if (___unusable)
                {
                    Bundle.Hud("fish.too-small");
                    return false;
                }
                bool valid = i is Fish f && f.fish.fishObject.canBeCooked;
                if (!valid) Bundle.Hud("fish.fail");

                var result = Entities.Items.Make(valid ? EntityType.FishCooked : EntityType.FishBurnt, other.transform.position);
                if (result.TryGetComponent(out Rigidbody rb))
                    rb.velocity = (NewMovement.Instance.transform.position - other.transform.position).normalized * 18f + Vector3.up * 10f;

                i.Kill(2, w => { w.Bool(false); w.Bool(true); });
            }
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(BaitItem), "OnTriggerEnter")]
    [HarmonyPrefix]
    static bool Bait(Collider other, BaitItem __instance, FishDB[] ___supportedWaters, FishObject[] ___attractFish, GameObject ___consumedPrefab, bool ___silentFail)
    {
        var agent = __instance.GetComponentInParent<Agent>();
        if (agent && Scene == "Level 5-S")
        {
            if (agent.Patron is Item i && i.IsOwner && other.TryGetComponent(out Water w) && w.fishDB)
            {
                if (___supportedWaters.Any(s => s == w.fishDB))
                {
                    Bundle.Hud("bait.took");

                    w.attractFish = ___attractFish;
                    Inst(___consumedPrefab, __instance.transform.position);

                    i.Kill();
                }
                else if (!___silentFail) Bundle.Hud("bait.nope");
            }
            return false;
        }
        else return true;
    }

    #endregion
}
