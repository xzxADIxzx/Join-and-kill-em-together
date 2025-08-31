namespace Jaket.Net.Types;

using System.Collections;
using UnityEngine;

using Jaket.Content;

/// <summary> Tangible entity of any plushie type. </summary>
public class Plushie : Item
{
    Agent agent;

    public Plushie(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => new(30f, 0f, 180f);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.GetComponent<ItemIdentifier>().onPickUp.onActivate.AddListener(() =>
        {
            if (Type == EntityType.xzxADIxzx) agent.StartCoroutine(ShakeYourHead(42));
            if (Type == EntityType.Sowler) agent.StartCoroutine(Hoot());
        });
    }

    #endregion
    #region other

    /// <summary> Special feature of the plushie of xzxADIxzx. </summary>
    public IEnumerator ShakeYourHead(int shakes)
    {
        var head = agent.transform.Find("Model/Head");

        while (shakes-- > 1)
        {
            head.localEulerAngles = new(90f * Random.Range(0, 3), 90f * Random.Range(0, 3), 90f * Random.Range(0, 3));
            yield return new WaitForSeconds(.4f / shakes);
        }

        head.localEulerAngles = new(270f, Random.value < .042f ? 45f : 0f, 0f);
    }

    /// <summary> Special feature of the plushie of OwlNotSowler. </summary>
    public IEnumerator Hoot()
    {
        yield return null; // TODO play different owl sounds from time to time
    }

    #endregion
}
