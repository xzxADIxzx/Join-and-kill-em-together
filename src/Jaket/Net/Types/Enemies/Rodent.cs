namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any rodent type. </summary>
public class Rodent : Enemy
{
    Agent agent;
    Float x, y, z, j;
    CancerousRodent scr;

    public Rodent(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 20;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Float(agent.Rotation.y);
        }
        else
        {
            w.Floats(x, y, z);
            w.Floats(j);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref j);
    }

    #endregion
    #region logic

    public override void Create() => Create(Entities.Enemies, ref x, ref y, ref z);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out scr);

        agent.transform.SetAsLastSibling(); // don't ask
    }

    public override void Update(float delta)
    {
        if (Lock(scr)) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(agent.Rotation.x,  j.GetAngle(delta), agent.Rotation.z );
    }

    #endregion
}
