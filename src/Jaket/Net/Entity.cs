namespace Jaket.Net;

using System;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Any entity that has updatable state synchronized across the network. </summary>
public abstract class Entity : MonoBehaviour
{
    /// <summary> Entity id in the global list. This is usually a small number, but for players, their account ids are used. </summary>
    public uint Id;
    /// <summary> Type of the entity, like a player or some kind of enemy. </summary>
    public EntityType Type;

    /// <summary> Id of the entity owner. </summary>
    public uint Owner;
    /// <summary> Whether the local player owns the entity. </summary>
    public bool IsOwner => Owner == Tools.AccId;

    /// <summary> Last update time via snapshots. </summary>
    public float LastUpdate;
    /// <summary> The number of updates read. </summary>
    public uint UpdatesCount;
    /// <summary> Whether the entity is dead. Dead entities will not be sync. </summary>
    public bool Dead;

    /// <summary> Different components, not inherent in all entities. </summary>
    public EnemyIdentifier EnemyId;
    public ItemIdentifier ItemId;
    public Animator Animator;
    public Rigidbody Rb;
    public Grenade Grenade;
    public Cannonball Ball;
    public LeviathanController Leviathan;
    public MinotaurChase Minotaur;

    /// <summary> Adds itself to the entities list if the player is the owner, and finds different components specific to different entities. </summary>
    protected void Init(Func<Entity, EntityType> prov, bool general = false, bool bullet = false, bool boss = false)
    {
        Log.Debug($"Initializing an entity with name {name}");

        // if an entity is marked with this tag, then it was downloaded over the network,
        // otherwise the entity is local and must be added to the global list
        if (name != "Net")
        {
            var provided = prov(this);
            if (provided == EntityType.None)
            {
                Log.Warning($"Couldn't find the entity type of the object {name}");
                Destroy(this);
                return;
            }

            Id = Entities.NextId();
            Type = provided;
            Owner = Tools.AccId;

            name = "Local";
            Networking.Entities[Id] = this;
        }

        #region components

        if (general)
        {
            EnemyId = GetComponent<EnemyIdentifier>();
            ItemId = GetComponent<ItemIdentifier>();
            Animator = GetComponentInChildren<Animator>();
            Rb = GetComponent<Rigidbody>();
        }
        if (bullet)
        {
            Grenade = GetComponent<Grenade>();
            Ball = GetComponent<Cannonball>();
        }
        if (boss)
        {
            Leviathan = GetComponent<LeviathanController>();
            Minotaur = GetComponent<MinotaurChase>();
        }

        #endregion
    }

    /// <summary> Updates the time of the last update and the number of updates. </summary>
    protected void Count()
    {
        LastUpdate = Time.time;
        UpdatesCount++;
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
        public float Last, Target;

        /// <summary> Updates interpolation values. </summary>
        public void Set(float value)
        {
            Last = Target;
            Target = value;
        }

        /// <summary> Reads values to be interpolated from the reader. </summary>
        public void Read(Reader r) => Set(r.Float());

        /// <summary> Returns an intermediate value. </summary>
        public float Get(float lastUpdate) => Mathf.Lerp(Last, Target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);

        /// <summary> Returns the intermediate value of the angle. </summary>
        public float GetAngel(float lastUpdate) => Mathf.LerpAngle(Last, Target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);
    }

    /// <summary> Class for finding entities according to their ID. </summary>
    public class EntityProv<T> where T : Entity
    {
        /// <summary> Id of the entity that needs to be found. </summary>
        public uint Id;

        private T value;
        public T Value => value?.Id == Id ? value : Networking.Entities.TryGetValue(Id, out var e) && e is T t ? value = t : null;
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

        Log.Debug($"Ownership of {this.Id} transferred from {Owner} to local player");
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
            Log.Debug($"Ownership of {this.Id} transferred from {Owner} to {id}");
            Owner = id;
            OnTransferred();
        }
    }
}
