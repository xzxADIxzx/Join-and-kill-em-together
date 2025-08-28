namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any type whose ownership can be transferred at any time. </summary>
public abstract class OwnableEntity : Entity
{
    /// <summary> Action to be performed upon transfer. </summary>
    public Runnable OnTransfer;
    /// <summary> Time of the last ownership transfer. </summary>
    public float LastTransfer;

    /// <summary> Whether the entity is locked, such entities' ownership cannot be transferred. </summary>
    public bool Locked
    {
        get => Time.time - LastTransfer < 1f;
        set => LastTransfer = value ? Time.time : float.PositiveInfinity;
    }

    public OwnableEntity(uint id, EntityType type) : base(id, type) { Locked = false; }

    /// <summary> Transfers the ownership to the local player. </summary>
    public void TakeOwnage()
    {
        if (IsOwner) return;
        if (Version.DEBUG) Log.Debug($"[ENTS] Transferred the ownership of {Id} from {Owner} to {AccId}");

        Owner = AccId;
        Locked = true;

        OnTransfer?.Invoke();
    }

    /// <summary> Writes the entity's owner into a snapshot. </summary>
    protected void WriteOwner(ref Writer w) => w.Id(Owner);

    /// <summary> Reads the entity's owner from a snapshot. </summary>
    protected bool ReadOwner(ref Reader r)
    {
        LastUpdate = Time.time;

        var id = r.Id();
        if (id != Owner && !Locked)
        {
            if (Version.DEBUG) Log.Debug($"[ENTS] Transferred the ownership of {Id} from {Owner} to {id}");

            Owner = id;
            Locked = true;

            OnTransfer?.Invoke();
        }
        return IsOwner;
    }
}
