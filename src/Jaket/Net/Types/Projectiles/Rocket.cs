namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the rocket type. </summary>
public class Rocket : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Grenade grenade;

    /// <summary> Whether the rocket is frozen. </summary>
    private bool frozen;
    /// <summary> Whether the player is riding. </summary>
    private bool riding;

    public Rocket(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 23;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Bool(grenade.frozen);
            w.Bool(grenade.playerRiding);
        }
        else
        {
            w.Floats(x, y, z);
            w.Bool(frozen);
            w.Bool(riding);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        frozen = r.Bool();
        riding = r.Bool();
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out grenade);
        agent.Rem<FloatingPointErrorPreventer>();

        OnTransfer = () =>
        {
            player = Owner;

            if (IsOwner)
                agent.Get(out grenade.rb);
            else
                grenade.rb = null;
        };

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        if (riding)
            player.Value?.Acquire(agent);
        else
        {
            if (agent.Parent != null) agent.Parent = null;
            agent.Position          = new(x.Get(delta),    y.Get(delta),    z.Get(delta)   );
            agent.transform.forward = new(x.Next - x.Prev, y.Next - y.Prev, z.Next - z.Prev);
        }
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);

        if (left >= 1) // harmless (environment), normal (entity), big (any beam), super (midair), ultra (malicious)
        {
            r.Bools(out var harmless, out var big, out var super, out var ultra, out _, out _, out _, out _);
            grenade.Explode(big, harmless, super, ultra ? 2f : 1f, ultra);
        }
    }

    #endregion
}
