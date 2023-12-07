namespace Jaket.IO;

using Steamworks;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

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
    public Reader(IntPtr memory, int length) { this.mem = memory; this.Length = length; }

    /// <summary> Reads data from the given memory via reader. </summary>
    public static void Read(IntPtr memory, int length, Action<Reader> cons) => cons(new(memory, length));

    /// <summary> Converts integer to float. </summary>
    public static unsafe float Int2Float(int value) => *(float*)(&value);
    /// <summary> Converts long to ulong. </summary>
    public static unsafe ulong Long2Ulong(long value) => *(ulong*)(&value);

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

    public byte Byte() => Marshal.ReadByte(mem, Inc(1));

    public byte[] Bytes(int amount)
    {
        var bytes = new byte[amount];
        Marshal.Copy(mem, bytes, Inc(amount), amount);
        return bytes;
    }

    public byte[] AllBytes()
    {
        Position = 0;
        return Bytes((int)Length);
    }

    public int Int() => Marshal.ReadInt32(mem, Inc(4));

    public float Float() => Int2Float(Marshal.ReadInt32(mem, Inc(4)));

    public string String() => Encoding.Unicode.GetString(Bytes(Int()));

    public Vector3 Vector() => new(Float(), Float(), Float());

    public Color32 Color() => new(Byte(), Byte(), Byte(), Byte());

    public SteamId Id() => Long2Ulong(Marshal.ReadInt64(mem, Inc(8)));

    public T Enum<T>() where T : Enum => (T)System.Enum.ToObject(typeof(T), Byte());

    #endregion
}
