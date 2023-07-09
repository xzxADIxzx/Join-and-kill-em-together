namespace Jaket.Net;

using System.IO;
using UnityEngine;

/// <summary> Any entity that has updatable state synchronized across the network. </summary>
public abstract class Entity : MonoBehaviour
{
    /// <summary> Entity id in the global list. </summary>
    public int Id;

    /// <summary> Type of entity, like a player or some kind of enemy. </summary>
    public Entities.Type Type;

    /// <summary> Last update time via snapshots. </summary>
    public float LastUpdate;

    public Entity()
    {
        Id = Networking.entities.Count;
    }

    /// <summary> Writes the entity data to the writer. </summary>
    public abstract void Write(BinaryWriter w);

    /// <summary> Reads the entity data from the writer. </summary>
    public abstract void Read(BinaryReader r);

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
        public void Read(BinaryReader r) => Set(r.ReadSingle());

        /// <summary> Returns an intermediate value. </summary>
        public float Get(float lastUpdate) => Mathf.Lerp(last, target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);

        /// <summary> Returns the intermediate value of the angle. </summary>
        public float GetAngel(float lastUpdate) => Mathf.LerpAngle(last, target, (Time.time - lastUpdate) / Networking.SNAPSHOTS_SPACING);
    }
}