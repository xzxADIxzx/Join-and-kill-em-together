namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any earthmover type. </summary>
public class Earthmover : Enemy
{
    Agent agent;
    Float x, y, z;
    global::Enemy enemy;

    /// <summary> Idols' state. </summary>
    private bool idol1, idol2;
    /// <summary> Idols' doors. </summary>
    private Door door1, door2;

    public Earthmover(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 11;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (Type != EntityType.Brain) return;
        if (IsOwner)
        {
            w.Bool(door1.open);
            w.Bool(door2.open);
        }
        else
        {
            w.Bool(idol1);
            w.Bool(idol2);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        if (Type == EntityType.Brain)
        {
            idol1 = r.Bool();
            idol2 = r.Bool();
        }
    }

    #endregion
    #region logic

    public override void Heal()
    {
        if (Type == EntityType.Brain)
            base.Heal();
        else
            enemy.health = PostHealth;
    }

    public override void Create()
    {
        // TODO other cases - earthmover's brain and security system
        if (Type == EntityType.RocketLauncher || Type == EntityType.Mortar || Type == EntityType.Tower) Assign(Entities.Enemies.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out enemy);

        if (Type == EntityType.Brain)
        {
            agent.transform.Find("../IdolPod/Cylinder"    ).TryGetComponent(out door1);
            agent.transform.Find("../IdolPod (1)/Cylinder").TryGetComponent(out door2);
        }
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        if (Type == EntityType.Brain)
        {
            if (door1.open != idol1) if (idol1) door1.Open(); else door1.Close();
            if (door2.open != idol2) if (idol2) door2.Open(); else door2.Close();
        }
    }

    public override void Killed(Reader r, int left)
    {
        Hidden = true; // TODO update enemy huh
        Dest(left >= 1 ? agent : agent.gameObject);
    }

    #endregion
}
