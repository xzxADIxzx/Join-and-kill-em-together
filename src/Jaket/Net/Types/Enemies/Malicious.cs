namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the malicious type. </summary>
public class Malicious : Enemy
{
    Agent agent;
    Float x, y, z, p, r;
    NavMeshAgent nma;
    MaliciousFace scr;
    Transform pr;

    public Malicious(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 27;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);

            w.Float(pr.eulerAngles.x);
            w.Float(pr.eulerAngles.y);

            w.Byte(Attack);
        }
        else
        {
            w.Floats(x, y, z);

            w.Float(r.Next);
            w.Float(r.Next);

            w.Byte(Attack);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);

        this.p.Set(r.Float());
        this.r.Set(r.Float());

        Attack = r.Byte();
    }

    #endregion
    #region logic

    public override bool Remain => true;

    public override void Create() => Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out nma);
        agent.Get(out scr);
        agent.Get(out pr, path: "MaliciousFace");
    }

    public override void Update(float delta)
    {
        if (Locked) { nma.enabled = false; scr.enabled = false; return; }

        scr.enabled = IsOwner;

        if (IsOwner) return;

        agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        pr.eulerAngles = new(p.GetAngle(delta), r.GetAngle(delta), 0f               );

        nma.enabled = false;
    }

    public override void Killed(bool explode)
    {
        base.Killed(explode);
        if (explode)
        {
            Hidden = true;
            scr.BreakCorpse();
        }
    }

    #endregion
}
