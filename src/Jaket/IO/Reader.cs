namespace Jaket.IO;

using System.Text;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

/// <summary>
/// Widely used structure that reads both basic and complex data types from unmanaged memory.
/// Be <b>extremely careful</b> as there is no memory bounds checking. 
/// </summary>
public unsafe struct Reader
{
    /// <summary> Pointer to the beginning of the allocated memory. </summary>
    public readonly Ptr Memory;
    /// <summary> Number of bytes read, sometimes the memory is not fully utilized. </summary>
    public int Position { get; private set; }

    /// <summary> Wraps the given memory into a reader. </summary>
    public Reader(Ptr memory) => Memory = memory;

    /// <summary> Moves the position by the given number of bytes. </summary>
    public void* Inc(int bytesCount)
    {
        Position += bytesCount;
        return (Memory + Position - bytesCount).ToPointer();
    }

    #region basic

    public bool Bool()             => *(byte*)Inc(1) == byte.MaxValue;

    public byte Byte()             => *(byte*)Inc(1);

    public uint Id()               => *(uint*)Inc(4);

    public int Int()               => *(int*)Inc(4);

    public float Float()           => *(float*)Inc(4);

    #endregion
    #region enums

    public PacketType PacketType()     => *(PacketType*)Inc(1);

    public EntityType EntityType()     => *(EntityType*)Inc(1);

    public Team Team()                 => *(Team*)Inc(1);

    #endregion
    #region complex

    public void Bools(out bool v0, out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7) { var value = Byte();
        v0 = (value & 1 << 0) != 0;
        v1 = (value & 1 << 1) != 0;
        v2 = (value & 1 << 2) != 0;
        v3 = (value & 1 << 3) != 0;
        v4 = (value & 1 << 4) != 0;
        v5 = (value & 1 << 5) != 0;
        v6 = (value & 1 << 6) != 0;
        v7 = (value & 1 << 7) != 0;
    }

    public void Bytes(byte[] value, int start, int count)
    {
        for (int i = start; i < start + count; i++) value[i] = *(byte*)Inc(1);
    }

    public void Bytes(byte[] value) => Bytes(value, 0, value.Length);

    public void Floats(ref Entity.Float x, ref Entity.Float y, ref Entity.Float z)
    {
        x.Set(*(float*)Inc(4));
        y.Set(*(float*)Inc(4));
        z.Set(*(float*)Inc(4));
    }

    public string String()
    {
        var bytes = new byte[Byte()];
        Bytes(bytes);
        return Encoding.ASCII.GetString(bytes);
    }

    public Vector3 Vector() => new(Float(), Float(), Float());

    public Color32 Color() => new(Byte(), Byte(), Byte(), Byte());

    public void Player(out Team team, out byte weapon, out byte emote, out byte rps, out bool typing)
    {
        short value = *(short*)Inc(2);

        weapon = (byte)(value >> 10 & 0b111111);
        team   = (Team)(value >>  7 & 0b111   );
        emote  = (byte)(value >>  3 & 0b1111  );
        rps    = (byte)(value >>  1 & 0b11    );
        typing =       (value >>  0 & 0b1     ) != 0;

        if (weapon == 0b111111) weapon = 0xFF;
        if (emote  == 0b1111)   emote  = 0xFF;
    }

    #endregion
}
