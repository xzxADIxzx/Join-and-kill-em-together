namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of the screwdriver type. </summary>
public class Screwdriver : OwnableEntity
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Renderer[] rs;

    public Screwdriver(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 33;

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
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(posX.Prev = posX.Next, posY.Prev = posY.Next, posZ.Prev = posZ.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out rs);

        OnTransfer = () =>
        {
            player = Owner;

            rb.isKinematic = !IsOwner;
            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                    r.material.color = team.Color() * 8f;
            });
        };

        Locked = false;

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(posX.Get(delta),      posY.Get(delta),      posZ.Get(delta)     );
        agent.Rotation = new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);
    }

    #endregion
}
