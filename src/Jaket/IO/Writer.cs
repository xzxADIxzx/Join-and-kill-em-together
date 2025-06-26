namespace Jaket.IO;

using System.Text;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

/// <summary>
/// Widely used structure that writes both basic and complex data types into unmanaged memory.
/// Be <b>extremely careful</b> as there is no memory bounds checking. 
/// </summary>
public unsafe struct Writer
{
    /// <summary> Pointer to the beginning of the allocated memory. </summary>
    public readonly Ptr Memory;
    /// <summary> Number of bytes written, often the memory is not fully utilized. </summary>
    public int Position { get; private set; }

    /// <summary> Wraps the given memory into a writer. </summary>
    public Writer(Ptr memory) => Memory = memory;

    /// <summary> Moves the position by the given number of bytes. </summary>
    public void* Inc(int bytesCount)
    {
        Position += bytesCount;
        return (Memory + Position - bytesCount).ToPointer();
    }

    #region basic

    public void Bool(bool value)   => *(byte*)Inc(1)  = value ? byte.MaxValue : byte.MinValue;

    public void Byte(byte value)   => *(byte*)Inc(1)  = value;

    public void Id(uint value)     => *(uint*)Inc(4)  = value;

    public void Int(int value)     => *(int*)Inc(4)   = value;

    public void Float(float value) => *(float*)Inc(4) = value;

    #endregion
    #region enums

    public void Enum(PacketType value) => *(PacketType*)Inc(1) = value;

    public void Enum(EntityType value) => *(EntityType*)Inc(1) = value;

    public void Enum(Team value)       => *(Team*)Inc(1)       = value;

    #endregion
    #region complex

    public void Bools(bool v0 = false, bool v1 = false, bool v2 = false, bool v3 = false, bool v4 = false, bool v5 = false, bool v6 = false, bool v7 = false) => Byte((byte)(
        (v0 ? 1 << 0 : 0) |
        (v1 ? 1 << 1 : 0) |
        (v2 ? 1 << 2 : 0) |
        (v3 ? 1 << 3 : 0) |
        (v4 ? 1 << 4 : 0) |
        (v5 ? 1 << 5 : 0) |
        (v6 ? 1 << 6 : 0) |
        (v7 ? 1 << 7 : 0)
    ));

    public void Bytes(byte[] value, int start, int length)
    {
        for (int i = start; i < length; i++) Byte(value[i]);
    }

    public void Bytes(byte[] value) => Bytes(value, 0, value.Length);

    public void Floats(Entity.Float x, Entity.Float y, Entity.Float z)
    {
        *(float*)Inc(4) = x.Next;
        *(float*)Inc(4) = y.Next;
        *(float*)Inc(4) = z.Next;
    }

    public void String(string value)
    {
        value ??= "";
        Byte((byte)(value.Length * 2));
        Bytes(Encoding.Unicode.GetBytes(value));
    }

    public void Vector(Vector3 value)
    {
        Float(value.x);
        Float(value.y);
        Float(value.z);
    }

    public void Color(Color32 value) => Int(value.rgba);

    public void Player(Team team, byte weapon, byte emote, byte rps, bool typing)
    {
        if (weapon == 0xFF) weapon = 0b111111;
        if (emote  == 0xFF) emote  = 0b1111;

        *(short*)Inc(2) = (short)((weapon << 10) | (((byte)team) << 7) | (emote << 3) | (rps << 1) | (typing ? 1 : 0));
    }

    #endregion
}
