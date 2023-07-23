namespace Jaket.Net;

using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Any entity that has updatable state synchronized across the network. </summary>
public abstract class Entity : MonoBehaviour
{
    /// <summary> Entity id in the global list. </summary>
    public int Id;

    /// <summary> Entity owner id, usually host, but RemotePlayers are owned by players. </summary>
    public SteamId Owner;

    /// <summary> Type of entity, like a player or some kind of enemy. </summary>
    public EntityType Type;

    /// <summary> Last update time via snapshots. </summary>
    public float LastUpdate;

    public Entity()
    {
        Id = Networking.Entities.Count;
        Owner = Networking.CurrentOwner;
    }

    /// <summary> Writes the entity data to the writer. </summary>
    public abstract void Write(Writer w);

    /// <summary> Reads the entity data from the reader. </summary>
    public abstract void Read(Reader r);

    /// <summary> Deals damage to the entity. </summary>
    public abstract void Damage(Reader r);

    /// <summary> Class for interpolating floating point values. </summary>
    public class FloatLerp
    {
        /// <summary> Interpolation values. </summary>
        private float last, target;

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