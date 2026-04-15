namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any type whose ownership can be transferred at any time. </summary>
public abstract class OwnableEntity : Entity
{
    Agent agent;

    /// <summary> Action to be performed upon transfer. </summary>
    public Runnable OnTransfer;
    /// <summary> Time of the last ownership transfer. </summary>
    public float LastTransfer;

    /// <summary> Whether the entity is locked, such entities' ownership cannot be transferred. </summary>
    public bool Locked
    {
        get => Time.time - LastTransfer < .4f;
        set => LastTransfer = value ? Time.time : float.NegativeInfinity;
    }
    /// <summary> Whether the entity is to be transferred to a new owner on the next snapshot. </summary>
    public uint TransferTo;

    public OwnableEntity(uint id, EntityType type) : base(id, type) { Locked = false; }

    #region logic

    public virtual string Name => $"{(IsOwner ? 'L' : 'R')}#{GetType().Name}";

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;
        agent.name = Name;
    }

    #endregion
    #region ownership

    /// <summary> Transfers the ownership to the local player. </summary>
    public void TakeOwnage()
    {
        if (IsOwner || Locked) return;
        if (Version.DEBUG) Log.Debug($"[ENTS] Transferred the ownership of {Id} from {Owner} to {AccId}");

        Owner = AccId;
        Locked = true;

        agent?.name = Name;
        OnTransfer?.Invoke();
    }

    /// <summary> Transfers the ownership to the given player. </summary>
    public void GiveOwnage(uint id)
    {
        if (Owner == id || Locked) return;
        if (Version.DEBUG) Log.Debug($"[ENTS] Transferred the ownership of {Id} from {Owner} to {id}");

        Owner = id;
        Locked = true;

        agent?.name = Name;
        OnTransfer?.Invoke();
    }

    /// <summary> Writes the entity's owner into a snapshot. </summary>
    protected void WriteOwner(ref Writer w)
    {
        if (TransferTo != 0u)
        {
            GiveOwnage(TransferTo);
            TransferTo = 0u;
        }
        w.Id(Owner);
    }

    /// <summary> Reads the entity's owner from a snapshot. </summary>
    protected bool ReadOwner(ref Reader r)
    {
        LastUpdate = Time.time;

        GiveOwnage(r.Id());
        return IsOwner;
    }

    #endregion
}
