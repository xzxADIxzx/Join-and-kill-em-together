namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;

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

    public Projectile(uint id, EntityType type, bool enableKm, bool disableKm) : base(id, type)
    {
        this.disableKm = disableKm;
        this.enableKm = enableKm;
    }

    #region logic

    public virtual void Paint(Renderer renderer)
    {
        if (renderer is TrailRenderer trailingTail)
            trailingTail.startColor = team.Color() with { a = .6f };
        else
            renderer.material.color = team.Color();

        if (renderer.material.IsKeywordEnabled("VERTEX_LIGHTING"))
        {
            renderer.material.DisableKeyword("VERTEX_LIGHTING");
            renderer.material.mainTexture = null;
        }
    }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out rb);
        agent.Get(out rs);
        agent.Get(out cs);
        agent.Run(() => { if (IsOwner) Kill(); }, 120f);

        OnTransfer = () =>
        {
            player = Owner;

            if (!IsOwner && enableKm) rb.isKinematic = true;
            if (IsOwner && disableKm) rb.isKinematic = false;

            rs.Each(Paint);
        };

        Locked = false;
        OnTransfer();
    }

    #endregion
    #region patch

    /// <summary> Invokes the patch logic if the provided object is an entity. </summary>
    public static bool Kill<T>(Component instance, Cons<T> patch) where T : Entity
    {
        if (instance.TryGetEntity(out T t))
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
