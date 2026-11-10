namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any projectile type. </summary>
public abstract class Rotatable : Projectile
{
    Agent agent;
    Float x, y, z, i, j, k;

    protected bool b0, b1;
    protected uint target;

    /// <summary> Whether to write two booleans or an identifier. </summary>
    private bool booleansOrId;

    public Rotatable(uint id, EntityType type, bool enableKm, bool disableKm, bool ignoreCl, bool booleansOrId) : base(id, type, enableKm, disableKm, ignoreCl)
    {
        this.booleansOrId = booleansOrId;
    }

    #region snapshot

    public override int BufferSize => booleansOrId ? 30 : 32;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);
        }
        else
        {
            w.Floats(x, y, z);
            w.Floats(i, j, k);
        }

        if (booleansOrId)
        {
            w.Bool(b0);
            w.Bool(b1);
        }
        else w.Id(target);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref i, ref j, ref k);

        if (booleansOrId)
        {
            b0 = r.Bool();
            b1 = r.Bool();
        }
        else target = r.Id();
    }
    #endregion
    #region logic

    public override void Create() => Create(Entities.Projectiles, ref x, ref y, ref z);

    public override void Assign(Agent agent) => base.Assign(this.agent = agent);

    public override void Update(float delta)
    {
        if (!IsOwner)
        {
            agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
            agent.Rotation = new(i.GetAngle(delta), j.GetAngle(delta), k.GetAngle(delta));
        }
    }

    #endregion
}
