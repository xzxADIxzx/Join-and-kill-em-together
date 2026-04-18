namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

using static Jaket.UI.Lib.Pal;

/// <summary> Abstract entity of any projectile type. </summary>
public abstract class Projectile : OwnableEntity
{
    Agent agent;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Renderer[] rs;
    Collider[] cs;

    /// <summary> Whether the kinematic mode should be toggled on transfer. </summary>
    private bool disableKm, enableKm;
    /// <summary> Whether the collision mode should be toggled on transfer. </summary>
    private bool ignoreCl;
    /// <summary> Position of the projectile shared among tangible classes. </summary>
    protected Float x, y, z;

    public Projectile(uint id, EntityType type, bool enableKm, bool disableKm, bool ignoreCl) : base(id, type)
    {
        this.disableKm = disableKm;
        this.enableKm = enableKm;
        this.ignoreCl = ignoreCl;
    }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public virtual void MasterKill()
    {
        if (IsOwner) Kill();
        if (Version.DEBUG) Log.Debug($"[ENTS] Killed an entity {Id} due to lifetime expiration");
    }

    public virtual void Paint(Renderer renderer)
    {
        if (renderer is TrailRenderer trailingTail)
        {
            trailingTail.startColor = team.Color() with { a = .6f };
            trailingTail.endColor   = white        with { a = .0f };
        }

        else if (renderer is SpriteRenderer sprite)
            sprite.color            = team.Color();
        else
            renderer.material.color = team.Color();

        if (renderer.material.IsKeywordEnabled("VERTEX_LIGHTING"))
        {
            renderer.material.DisableKeyword("VERTEX_LIGHTING");
            renderer.material.mainTexture = null;
        }
    }

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Init, y.Init, z.Init)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out rb);
        agent.Get(out rs);
        agent.Get(out cs);
        agent.Run(MasterKill, 90f);

        OnTransfer = () =>
        {
            player = Owner;

            if (!IsOwner && enableKm && rb) rb.isKinematic = true;
            if (IsOwner && disableKm && rb) rb.isKinematic = false;

            rs.Each(Paint);
            UpdateIgnore();
        };

        Locked = false;
        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (!IsOwner) agent.Position = new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
    }

    public virtual void UpdateIgnore()
    {
        if (ignoreCl) Networking.Entities.Player(p => cs.Each(c => p.Toggle(c)));
    }

    public override void Damage(Reader r) { }

    #endregion
    #region patch

    /// <summary> Invokes the patch logic if the provided object is an entity. </summary>
    public static bool Kill<T>(Component instance, Cons<T> patch, bool onlyAlive = false) where T : Entity
    {
        if (instance.TryGetEntity(out T t) && !(onlyAlive && t.Hidden))
        {
            patch(t);
            return false;
        }
        else return true;
    }

    /// <summary> Invokes the patch logic if the hitten collider is an entity. </summary>
    public static bool Deal<T>(Component instance, Patch<T> patch, Agent agent = null, Collider other = null, EnemyIdentifier eid = null) where T : Entity
    {
        if (instance.TryGetEntity(out T t))
        {
            if (!eid && !other.TryGetComponent(out eid)) eid = other.GetComponent<EnemyIdentifierIdentifier>()?.eid;
            if (!eid || !eid.TryGetComponent(out agent)) return t.IsOwner;

            if (t.IsOwner) return patch(eid, agent.Patron.Id, agent.Patron is RemotePlayer p && p.Team.Ally(), t);
            return false;
        }
        else return true;
    }

    /// <summary> Patch logic to be executed when an entity is hitten. </summary>
    public delegate bool Patch<T>(EnemyIdentifier eid, uint tid, bool ally, T t);

    #endregion
}
