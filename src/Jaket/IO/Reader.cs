namespace Jaket.IO;

using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

/// <summary>
/// Widely used structure that reads both basic and complex data types from unmanaged memory.
/// Be <b>extremely careful</b> as there is no memory bounds check. 
/// </summary>
public unsafe struct Reader
{
    /// <summary> Pointer to the beginning of the allocated memory. </summary>
    public readonly Ptr Memory;
    /// <summary> Pointer to the beginning of the reading position. </summary>
    private nint position;

    /// <summary> Wraps the given memory pointer into a reader. </summary>
    public Reader(Ptr memory) => position = (Memory = memory);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Inc(nint bytesCount) => (void*)((position += bytesCount) - bytesCount);

    #region basic

    public bool  Bool () => *(byte *)Inc(1) == byte.MaxValue;

    public byte  Byte () => *(byte *)Inc(1);

    public uint  Id   () => *(uint *)Inc(4);

    public int   Int  () => *(int  *)Inc(4);

    public float Float() => *(float*)Inc(4);

    #endregion
    #region enums

    public PacketType PacketType() => *(PacketType*)Inc(1);

    public EntityType EntityType() => *(EntityType*)Inc(1);

    public Team       Team      () => *(Team      *)Inc(1);

    #endregion
    #region complex

    public void Bools(out bool b0, out bool b1, out bool b2, out bool b3, out bool b4, out bool b5, out bool b6, out bool b7)
    {   var value = Byte();
        b0 = (value & 1 << 0) != 0;
        b1 = (value & 1 << 1) != 0;
        b2 = (value & 1 << 2) != 0;
        b3 = (value & 1 << 3) != 0;
        b4 = (value & 1 << 4) != 0;
        b5 = (value & 1 << 5) != 0;
        b6 = (value & 1 << 6) != 0;
        b7 = (value & 1 << 7) != 0;
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

    public void Floats(ref Entity.Float r)
    {
        r.Set(*(float*)Inc(4));
    }

    public string String()
    {
        var bytes = new byte[Byte()];
        Bytes(bytes);
        return Encoding.ASCII.GetString(bytes);
    }

    public Vector3 Vector() => *(Vector3*)Inc(12);

    public Vector2 Point() => *(Vector2*)Inc(8);

    public Color32 Color() => *(Color32*)Inc(4);

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
