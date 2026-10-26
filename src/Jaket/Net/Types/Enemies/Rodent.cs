namespace Jaket.Net.Types;

using Jaket.Content;

/// <summary> Tangible entity of any rodent type. </summary>
public class Rodent : Foe
{
    Agent agent;
    CancerousRodent scr;

    public Rodent(uint id, EntityType type) : base(id, type, false) { }

    #region logic

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out scr);

        agent.transform.SetAsLastSibling(); // don't ask
    }

    public override void Update(float delta)
    {
        if (IsOwner) Moving = false;
        if (Lock(scr)) return;

        base.Update(delta);
    }

    #endregion
}
