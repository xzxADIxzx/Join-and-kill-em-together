namespace Jaket.IO;

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

using Jaket.Content;

/// <summary> Wrapper over Marshal for convenience and the ability to read floating point numbers. </summary>
public class Reader
{
    /// <summary> Current cursor position. </summary>
    public int Position;
    /// <summary> Allocated memory length. </summary>
    public readonly int Length;
    /// <summary> Pointer to the allocated memory. </summary>
    public readonly IntPtr mem;

    /// <summary> Creates a reader with the given memory. </summary>
    public Reader(IntPtr memory, int length) { mem = memory; Length = length; }

    /// <summary> Reads data from the given memory via reader. </summary>
    public static void Read(IntPtr memory, int length, Action<Reader> cons) => cons(new(memory, length));

    /// <summary> Converts integer to float. </summary>
    public static unsafe float Int2Float(int value) => *(float*)&value;
    /// <summary> Converts int to uint. </summary>
    public static unsafe uint Int2Uint(int value) => *(uint*)&value;

    /// <summary> Moves the cursor by a given number of bytes and returns the old cursor position. </summary>
    public int Inc(int amount)
    {
        if (Position < 0) throw new IndexOutOfRangeException("Attempt to read data at a negative index.");
        Position += amount;

        if (Position > Length) throw new IndexOutOfRangeException("Attempt to read more bytes than were allocated in memory.");
        return Position - amount;
    }

    #region types

    public bool Bool() => Marshal.ReadByte(mem, Inc(1)) == 0xFF;

    public void Bools(out bool v0, out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7)
    {
        byte value = Byte();
        v0 = (value & 1 << 0) != 0; v1 = (value & 1 << 1) != 0; v2 = (value & 1 << 2) != 0; v3 = (value & 1 << 3) != 0;
        v4 = (value & 1 << 4) != 0; v5 = (value & 1 << 5) != 0; v6 = (value & 1 << 6) != 0; v7 = (value & 1 << 7) != 0;
    }

    public byte Byte() => Marshal.ReadByte(mem, Inc(1));

    public byte[] Bytes(int amount) => Bytes(0, amount);

    public byte[] Bytes(int start, int amount)
    {
        var bytes = new byte[amount];
        Marshal.Copy(mem + Inc(amount), bytes, start, amount);
        return bytes;
    }

    public int Int() => Marshal.ReadInt32(mem, Inc(4));

    public float Float() => Int2Float(Marshal.ReadInt32(mem, Inc(4)));

    public uint Id() => Int2Uint(Marshal.ReadInt32(mem, Inc(4)));

    public string String() => Encoding.Unicode.GetString(Bytes(Int()));

    public Vector3 Vector() => new(Float(), Float(), Float());

    public Color32 Color() => new(Byte(), Byte(), Byte(), Byte());

    public T Enum<T>() where T : Enum => (T)System.Enum.ToObject(typeof(T), Byte());

    public void Player(out Team team, out byte weapon, out byte emoji, out byte rps, out bool typing)
    {
        short value = Marshal.ReadInt16(mem, Inc(2));

        weapon = (byte)(value >> 10 & 0b111111);
        team = (Team)(value >> 7 & 0b111);
        emoji = (byte)(value >> 3 & 0b1111);
        rps = (byte)(value >> 1 & 0b11);
        typing = (value & 1) != 0;

        if (weapon == 0b111111) weapon = 0xFF;
        if (emoji == 0b1111) emoji = 0xFF;
    }

    #endregion
}
