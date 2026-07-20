namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of any fish type. </summary>
public class Fish : Item
{
    ItemIdentifier itemId;
    FishObjectReference fish;
    ObjectActivator timer;

    public Fish(uint id, EntityType type) : base(id, type) { }

    #region properties

    public override Vector3 HoldRotation => new(10f, 230f, 110f);

    #endregion
    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(agent);

        agent.Get(out itemId);
        agent.Get(out fish);
        agent.Rem<ExplosiveFish>(true);

        FishManager.Instance.UnlockFish(fish.fishObject);
    }

    public override void Update(float delta)
    {
        base.Update(delta);

        if (Type == EntityType.FishBomb && !itemId.pickedUp && !timer) timer = fish.Component<ObjectActivator>(a =>
        {
            a.ActivateDelayed(3f);
            a.events = new() { onActivate = new() };
            a.events.onActivate.AddListener(() =>
            {
                Killed(default, -1);
                Inst(Entities.Vendor.Prefabs[(byte)EntityType.Harmless], fish.transform.position);
            });
            fish.transform.Find("Bomb Fish/Fire")?.gameObject.SetActive(true);
        });
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(FishCooker), nameof(FishCooker.OnTriggerEnter))]
    [Prefix]
    static bool Cook(Collider other, bool ___unusable)
    {
        var agent = other.GetComponentInParent<Agent>();
        if (agent)
        {
            if (agent.Patron is Fish f && f.IsOwner && f.Type != EntityType.FishCooked && f.Type != EntityType.FishBurnt)
            {
                if (___unusable)
                {
                    Bundle.Hud("fish.too-small");
                    return false;
                }
                bool valid = f.fish.fishObject.canBeCooked;
                if (!valid) Bundle.Hud("fish.fail");

                var result = Entities.Items.Make(valid ? EntityType.FishCooked : EntityType.FishBurnt, other.transform.position);
                if (result.TryGetComponent(out Rigidbody rb))
                    rb.velocity = (NewMovement.Instance.transform.position - other.transform.position).normalized * 18f + Vector3.up * 10f;

                f.Kill(1, w => w.Bools(false, true));
            }
            return false;
        }
        else return true;
    }

    [DynamicPatch(typeof(BaitItem), nameof(BaitItem.OnTriggerEnter))]
    [Prefix]
    static bool Bait(Collider other, BaitItem __instance, FishDB[] ___supportedWaters, FishObject[] ___attractFish, GameObject ___consumedPrefab, bool ___silentFail)
    {
        var agent = __instance.GetComponentInParent<Agent>();
        if (agent)
        {
            if (agent.Patron is Item i && i.IsOwner && other.TryGetComponent(out Water w) && w.fishDB)
            {
                if (___supportedWaters.Has(w.fishDB))
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
