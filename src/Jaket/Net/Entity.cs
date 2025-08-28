namespace Jaket.Net;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any type whose state is synchronized across the network via snapshots. </summary>
public abstract class Entity
{
    /// <summary> Unique identifier of the entity, account ids are used for players. </summary>
    public readonly uint Id;
    /// <summary> Type of the entity, such as player or enemy of some kind. </summary>
    public readonly EntityType Type;

    /// <summary> Account identifier of the entity's owner. </summary>
    public uint Owner;
    /// <summary> Whether the local player owns the entity. </summary>
    public bool IsOwner => Owner == AccId;

    /// <summary> Time of the last snapshot reception. </summary>
    public float LastUpdate;
    /// <summary> Time of the last entity concealment. </summary>
    public float LastHidden;

    /// <summary> Whether the entity is hidden, such entities are neither synchronized nor updated. </summary>
    public bool Hidden
    {
        get => LastHidden != float.PositiveInfinity;
        set => LastHidden = value ? Time.time : float.PositiveInfinity;
    }

    public Entity(uint id, EntityType type) { Owner = Id = id; Type = type; Hidden = false; }

    /// <summary> Pushes the entity into networking pool. </summary>
    public void Push() => Networking.Entities[Id] = this;

    /// <summary> Kills the entity remotely and, if necessary, locally. </summary>
    public void Kill(int bytesCount = 0, Cons<Writer> data = null, bool locally = true)
    {
        Networking.Send(PacketType.Death, 4 + bytesCount, w =>
        {
            w.Id(Id);
            data?.Invoke(w);
        });
        if (locally) Killed(default, -1);
    }

    /// <summary> Number of bytes that the entity takes in a snapshot, plus the size of its header. </summary>
    public abstract int BufferSize { get; }
    /// <summary> Writes the entity data into a snapshot. </summary>
    public abstract void Write(Writer w);
    /// <summary> Reads the entity data from a snapshot. </summary>
    public abstract void Read(Reader r);

    /// <summary> Creates an object for manipulation. </summary>
    public abstract void Create();
    /// <summary> Assigns the given agent to the entity. </summary>
    public abstract void Assign(Agent agent);
    /// <summary> Updates internal logic of the entity. </summary>
    public abstract void Update(float delta);
    /// <summary> Deals incoming damage to the entity. </summary>
    public abstract void Damage(Reader r);
    /// <summary> Kills the entity, takes custom data. </summary>
    public abstract void Killed(Reader r, int left);

    /// <summary> Most of the entities manipulate an object of some kind, agents implement these interactions. </summary>
    public class Agent : MonoBehaviour
    {
        /// <summary> Entity that owns the agent and has to be updated every frame. </summary>
        public Entity Patron;

        public Transform Parent
        {
            get => transform.parent;
            set => transform.parent = value;
        }
        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }
        public Vector3 Rotation
        {
            get => transform.eulerAngles;
            set => transform.eulerAngles = value;
        }
        public Vector3 Scale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        private void Update() => Stats.MeasureTime(ref Stats.EntityMs, () => Patron.Update(Time.time - Patron.LastUpdate));
    }

    /// <summary> Widely used structure that interpolates floating point numbers. </summary>
    public struct Float
    {
        /// <summary> Numbers to interpolate. </summary>
        public float Prev, Next;

        /// <summary> Updates the boundaries. </summary>
        public void Set(float value)
        {
            Prev = Next;
            Next = value;
        }

        /// <summary> Returns an intermediate value. </summary>
        public readonly float Get(float delta) => Mathf.Lerp(Prev, Next, delta * Networking.TICKS_PER_SECOND);

        /// <summary> Returns an intermediate value, taking into account the cyclic nature of angles. </summary>
        public readonly float GetAngle(float delta) => Mathf.LerpAngle(Prev, Next, delta * Networking.TICKS_PER_SECOND);
    }

    /// <summary> Widely used structure that finds and caches entities. </summary>
    public struct Cache<T> where T : Entity
    {
        /// <summary> Identifier of the entity to find. </summary>
        public uint Id;

        private T value;
        public T Value => value ?? (Networking.Entities[Id] is T t ? value = t : null);

        public static implicit operator uint(Cache<T> ch) => ch.Id;
        public static implicit operator Cache<T>(uint id) => default(Cache<T>) with { Id = id };
    }
}
