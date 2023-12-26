namespace Jaket.Net;

using System;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Any entity that has updatable state synchronized across the network. </summary>
public abstract class Entity : MonoBehaviour
{
    /// <summary> Entity id in the global list. This is usually a small number, but for players, their account ids are used. </summary>
    public ulong Id;
    /// <summary> Type of entity, like a player or some kind of enemy. </summary>
    public EntityType Type;

    /// <summary> Enemy component, not inherent in all entities. </summary>
    public EnemyIdentifier EnemyId;
    /// <summary> Item component, not inherent in all entities. </summary>
    public ItemIdentifier ItemId;
    /// <summary> Animator component, not inherent in all entities. </summary>
    public Animator Animator;

    /// <summary> Last update time via snapshots. </summary>
    public float LastUpdate;

    /// <summary> Adds itself to the entities list if the player is the host, and finds different components specific to different entities. </summary>
    protected virtual void Init(Func<Entity, EntityType> prov, Func<bool> remote = null)
    {
        EnemyId = GetComponent<EnemyIdentifier>();
        ItemId = GetComponent<ItemIdentifier>();
        Animator = GetComponentInChildren<Animator>();

        if (remote == null ? LobbyController.IsOwner : !remote())
        {
            var provided = prov(this);
            if (provided == EntityType.None)
            {
                Destroy(this);
                return;
            }

            this.Id = Entities.NextId();
            this.Type = provided;

            Networking.Entities[Id] = this;
        }

        // rename the object to prevent object looping
        gameObject.name = "Net";
    }

    /// <summary> Writes the entity data to the writer. </summary>
    public abstract void Write(Writer w);
    /// <summary> Reads the entity data from the reader. </summary>
    public abstract void Read(Reader r);

    /// <summary> Deals damage to the entity. </summary>
    public virtual void Damage(Reader r) => Bullets.DealDamage(EnemyId, r);
    /// <summary> Kills the entity. </summary>
    public virtual void Kill() => Destroy(gameObject);

    /// <summary> Class for interpolating floating point values. </summary>
    public class FloatLerp
    {
        /// <summary> Interpolation values. </summary>
        public float last, target;

        /// <summary> Updates interpolation values. </summary>
        public void Set(float value)
        {
            last = target;
            target = value;
        }

        /// <summary> Reads values to be interpolated from the reader. </summary>
        public void Read(Reader r) => Set(r.Float());

        /// <summary> Returns an intermediate value. </summary>
        public float Get(float lastUpdate) => Mathf.Lerp(last, target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);

        /// <summary> Returns the intermediate value of the angle. </summary>
        public float GetAngel(float lastUpdate) => Mathf.LerpAngle(last, target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);
    }
}

/// <summary> Entity that has an owner and can be passed from client to client. </summary>
public abstract class OwnableEntity : Entity
{
    /// <summary> Id of the entity owner. </summary>
    public ulong Owner = LobbyController.IsOwner ? Networking.LocalPlayer.Id : 0L;
    /// <summary> Whether the player owns the entity. </summary>
    public bool IsOwner => Owner == Networking.LocalPlayer.Id;

    /// <summary> Time of last transfer of the entity from one client to another. </summary>
    public float LastTransferTime { get; protected set; }
    /// <summary> Event triggered when ownership of the entity is transferred. </summary>
    public Action OnTransferred;

    protected override void Init(Func<Entity, EntityType> prov, Func<bool> remote = null)
    {
        base.Init(prov, remote);
        OnTransferred = () => LastTransferTime = Time.time;
    }

    /// <summary> Transfers ownership of the entity to the local player. </summary>
    public void TakeOwnage()
    {
        if (IsOwner) return;

        Owner = Networking.LocalPlayer.Id;
        OnTransferred();
    }

    public override void Write(Writer w) => w.Id(Owner);

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        ulong id = r.Id(); // this is necessary so that clients don't fight for ownership of the entity
        if (Owner != id && Time.time > LastTransferTime + 1f)
        {
            Owner = id;
            OnTransferred();
        }
    }
}
