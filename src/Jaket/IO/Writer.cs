namespace Jaket.IO;

using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

/// <summary>
/// Widely used structure that writes both basic and complex data types into unmanaged memory.
/// Be <b>extremely careful</b> as there is no memory bounds check. 
/// </summary>
public unsafe struct Writer
{
    /// <summary> Pointer to the beginning of the allocated memory. </summary>
    public readonly Ptr Memory;
    /// <summary> Pointer to the beginning of the writing position. </summary>
    private nint position;

    /// <summary> Wraps the given memory pointer into a writer. </summary>
    public Writer(Ptr memory) => position = (Memory = memory);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Inc(nint bytesCount) => (void*)((position += bytesCount) - bytesCount);

    #region basic

    public void Bool (bool  value) => *(byte *)Inc(1) = value ? byte.MaxValue : byte.MinValue;

    public void Byte (byte  value) => *(byte *)Inc(1) = value;

    public void Id   (uint  value) => *(uint *)Inc(4) = value;

    public void Int  (int   value) => *(int  *)Inc(4) = value;

    public void Float(float value) => *(float*)Inc(4) = value;

    #endregion
    #region enums

    public void Enum(PacketType value) => *(PacketType*)Inc(1) = value;

    public void Enum(EntityType value) => *(EntityType*)Inc(1) = value;

    public void Enum(Team       value) => *(Team      *)Inc(1) = value;

    #endregion
    #region complex

    public void Bools(bool b0 = false, bool b1 = false, bool b2 = false, bool b3 = false, bool b4 = false, bool b5 = false, bool b6 = false, bool b7 = false) => Byte((byte)
    (
        (b0 ? 1 << 0 : 0) |
        (b1 ? 1 << 1 : 0) |
        (b2 ? 1 << 2 : 0) |
        (b3 ? 1 << 3 : 0) |
        (b4 ? 1 << 4 : 0) |
        (b5 ? 1 << 5 : 0) |
        (b6 ? 1 << 6 : 0) |
        (b7 ? 1 << 7 : 0)
    ));

    public void Bytes(byte[] value, int start, int count)
    {
        for (int i = start; i < start + count; i++) *(byte*)Inc(1) = value[i];
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
        var bytes = Encoding.ASCII.GetBytes(value ?? "");
        Byte((byte)bytes.Length);
        Bytes(bytes);
    }

    public void Vector(Vector3 value) => *(Vector3*)Inc(12) = value;

    public void Point(Vector2 value) => *(Vector2*)Inc(8) = value;

    public void Color(Color32 value) => *(Color32*)Inc(4) = value;

    public void Player(Team team, byte weapon, byte emote, byte rps, bool typing)
    {
        if (weapon == 0xFF) weapon = 0b111111;
        if (emote  == 0xFF) emote  = 0b1111;

        *(short*)Inc(2) = (short)((weapon << 10) | (((byte)team) << 7) | (emote << 3) | (rps << 1) | (typing ? 1 : 0));
    }

    #endregion
}
