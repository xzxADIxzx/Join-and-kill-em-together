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
    /// <summary> The number of updates written. </summary>
    public uint UpdatesCount;
    /// <summary> Whether the entity is dead. Dead entities will not be sync. </summary>
    public bool Dead;

    /// <summary> Different components, not inherent in all entities. </summary>
    public EnemyIdentifier EnemyId;
    public ItemIdentifier ItemId;
    public Animator Animator;
    public Rigidbody Rb;

    /// <summary> Adds itself to the entities list if the player is the owner, and finds different components specific to different entities. </summary>
    protected void Init(Func<Entity, EntityType> prov, bool getGeneralComponents = false)
    {
        Log.Debug($"Initializing an entity with name {name}");

        // do this before calling prov, so as not to break the search of the entity type
        EnemyId = GetComponent<EnemyIdentifier>();
        ItemId = GetComponent<ItemIdentifier>();

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

        if (getGeneralComponents)
        {
            Animator = GetComponentInChildren<Animator>();
            Rb = GetComponent<Rigidbody>();
        }
    }

    /// <summary> Teleports the entity to the target position and clears its trail. </summary>
    protected void ClearTrail(TrailRenderer trail, params FloatLerp[] l)
    {
        if (IsOwner) return;
        transform.position = new(l[0].Last = l[0].Target, l[1].Last = l[1].Target, l[2].Last = l[2].Target);
        trail.Clear();
    }

    /// <summary> Writes the entity data to the writer. </summary>
    public abstract void Write(Writer w);
    /// <summary> Reads the entity data from the reader. </summary>
    public abstract void Read(Reader r);

    /// <summary> Deals damage to the entity. </summary>
    public virtual void Damage(Reader r) => Bullets.DealDamage(EnemyId, r);
    /// <summary> Kills the entity. </summary>
    public virtual void Kill(Reader r) => Dead = true;

    /// <summary> Kills the entity and informs all the network members about it. </summary>
    public void NetKill()
    {
        Kill(null);
        Networking.Send(PacketType.KillEntity, w => w.Id(Id), size: 4);
    }

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
