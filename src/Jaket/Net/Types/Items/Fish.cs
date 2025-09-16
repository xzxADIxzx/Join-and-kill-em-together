namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

/// <summary> Tangible entity of any fish type. </summary>
public class Fish : Item
{
    FishObjectReference fish;

    public Fish(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => new(10f, 230f, 110f);

    public override void Assign(Agent agent)
    {
        base.Assign(agent);

        agent.TryGetComponent(out fish);

        FishManager.Instance.UnlockFish(fish.fishObject);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(FishCooker), "OnTriggerEnter")]
    [HarmonyPrefix]
    static bool Cook(FishCooker __instance, Collider other, bool ___unusable)
    {
        var agent = other.GetComponentInParent<Agent>();
        if (agent && Scene == "Level 5-S")
        {
            if (agent.Patron is Item i && i.IsOwner && i.Type != EntityType.FishCooked && i.Type != EntityType.FishBurnt)
            {
                if (___unusable)
                {
                    if (!HudMessageReceiver.Instance.text.enabled) Bundle.Hud("fish.too-small");
                    return false;
                }
                bool valid = i is Fish f && f.fish.fishObject.canBeCooked;
                if (!valid) Bundle.Hud("fish.failure");

                var result = Entities.Items.Make(valid ? EntityType.FishCooked : EntityType.FishBurnt, other.transform.position);
                if (result.TryGetComponent(out Rigidbody rb))
                    rb.velocity = (NewMovement.Instance.transform.position - other.transform.position).normalized * 18f + Vector3.up * 10f;

                i.Kill(2, w => { w.Bool(false); w.Bool(true); });
            }
            return false;
        }
        else return true;
    }

    #endregion
}
