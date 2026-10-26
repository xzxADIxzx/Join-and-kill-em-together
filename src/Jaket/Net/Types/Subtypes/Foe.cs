namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any enemy type. </summary>
public abstract class Foe : Enemy
{
    Agent agent;
    Float x, y, z, i, j, k;

    /// <summary> Whether the rotation is axes. </summary>
    private bool axes;

    public Foe(uint id, EntityType type, bool axes) : base(id, type)
    {
        this.axes = axes;
    }

    #region snapshot

    public override int BufferSize => axes ? 26 : 22;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            if (axes)
            {
                w.Vector(DrawPos);
                w.Point(DrawRot);
            }
            else
            {
                w.Vector(DrawPos);
                w.Float(DrawRot.y);
            }
        }
        else
        {
            if (axes)
            {
                w.Floats(x, y, z);
                w.Floats(i, j);
            }
            else
            {
                w.Floats(x, y, z);
                w.Floats(j);
            }
        }

        w.Byte(Attack);
        w.Bool(Moving);

        if (IsOwner)
        {
            if (LastAttack != Attack) LastAttack = Attack;
            else LastAttack = Attack = 0;
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        if (axes)
        {
            r.Floats(ref x, ref y, ref z);
            r.Floats(ref i, ref j);
        }
        else
        {
            r.Floats(ref x, ref y, ref z);
            r.Floats(ref j);
        }

        Attack = r.Byte();
        Moving = r.Bool();
    }

    #endregion
    #region logic

    public override void Create() => Create(Entities.Enemies, ref x, ref y, ref z);

    public override void Assign(Agent agent) => base.Assign(this.agent = agent);

    public override void Update(float delta)
    {
        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = new(i.GetAngle(delta), j.GetAngle(delta), k.GetAngle(delta));
    }

    #endregion
}
