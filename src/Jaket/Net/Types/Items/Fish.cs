namespace Jaket.Net.Types;

using UnityEngine;

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
}
