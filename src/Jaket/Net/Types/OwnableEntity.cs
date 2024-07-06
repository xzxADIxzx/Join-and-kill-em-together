namespace Jaket.Net.Types;

using System;
using UnityEngine;

using Jaket.IO;
using Jaket.Content;

/// <summary> Entity whose ownership can be transferred to another client at any time. </summary>
public abstract class OwnableEntity : Entity
{
    /// <summary> Time of last transfer of the entity from one client to another. </summary>
    public float LastTransferTime { get; protected set; }
    /// <summary> Event triggered when the ownership of the entity is transferred. </summary>
    public Action OnTransferred;

    /// <summary> Initializes the event of the transfer of the right to own the object. </summary>
    protected void InitTransfer(Action on = null) => OnTransferred = () =>
    {
        LastTransferTime = Time.time;
        on?.Invoke();

        // this will help in the future determine who owns this entity
        name = IsOwner ? "Local" : "Net";
    };

    /// <summary> Transfers ownership of the entity to the local player. </summary>
    public void TakeOwnage()
    {
        if (IsOwner) return;

        if (Enemies.Debug) Log.Debug($"Ownership of {Id} transferred from {Owner} to the local player");
        Owner = Tools.AccId;
        OnTransferred();
    }

    public override void Write(Writer w) { UpdatesCount++; w.Id(Owner); }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        uint id = r.Id(); // this is necessary so that clients don't fight for ownership of the entity
        if (Owner != id && Time.time > LastTransferTime + 1f)
        {
            if (Enemies.Debug) Log.Debug($"Ownership of {Id} transferred from {Owner} to {id}");
            Owner = id;
            OnTransferred();
        }
    }
}
