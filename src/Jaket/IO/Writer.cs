namespace Jaket.IO;

using Steamworks;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary> Wrapper over Marshal for convenience and the ability to write floating point numbers. </summary>
public class Writer
{
    /// <summary> Current cursor position. </summary>
    public int Position;
    /// <summary> Allocated memory length. </summary>
    public readonly int Length;

    /// <summary> Pointer to the allocated memory. </summary>
    public readonly IntPtr mem;
    /// <summary> Creates a writer with the given memory. </summary>
    public Writer(IntPtr memory, int length) { this.mem = memory; this.Length = length; }

    /// <summary> Allocates memory and writes data there. </summary>
    public static void Write(Action<Writer> cons, Action<IntPtr, int> result, int memoryAmount = 64)
    {
        Writer instance = new(Marshal.AllocHGlobal(memoryAmount), memoryAmount);
        cons(instance);
        result(instance.mem, instance.Position); // 64 bytes are allocated in memory by default, which is enough for each entity, but not all of this memory is used
        Pointers.Add(instance.mem);
    }

    /// <summary> Converts float to integer. </summary>
    public static unsafe int Float2Int(float value) => *(int*)(&value);
    /// <summary> Converts ulong to long. </summary>
    public static unsafe long Ulong2long(ulong value) => *(long*)(&value);

    /// <summary> Moves the cursor by a given number of bytes and returns the old cursor position. </summary>
    public int Inc(int amount)
    {
        if (Position < 0) throw new IndexOutOfRangeException("Attempt to write data at a negative index.");
        Position += amount;

        if (Position > Length) throw new IndexOutOfRangeException("Attempt to write more bytes than were allocated in memory.");
        return Position - amount;
    }

    #region types

    public void Bool(bool value) => Marshal.WriteByte(mem, Inc(1), (byte)(value ? 0xFF : 0x00));

    public void Bools(bool v0 = false, bool v1 = false, bool v2 = false, bool v3 = false, bool v4 = false, bool v5 = false, bool v6 = false, bool v7 = false) =>
        Byte((byte)((v0 ? 1 : 0) | (v1 ? 1 << 1 : 0) | (v2 ? 1 << 2 : 0) | (v3 ? 1 << 3 : 0) | (v4 ? 1 << 4 : 0) | (v5 ? 1 << 5 : 0) | (v6 ? 1 << 6 : 0) | (v7 ? 1 << 7 : 0)));

    public void Byte(byte value) => Marshal.WriteByte(mem, Inc(1), value);

    public void Bytes(byte[] value) => Marshal.Copy(value, 0, mem + Inc(value.Length), value.Length);

    public void Bytes(byte[] value, int start, int length) => Marshal.Copy(value, start, mem + Inc(length), length);

    public void Int(int value) => Marshal.WriteInt32(mem, Inc(4), value);

    public void Float(float value) => Marshal.WriteInt32(mem, Inc(4), Float2Int(value));

    public void String(string value)
    {
        value ??= "";
        Int(value.Length * 2);
        Bytes(Encoding.Unicode.GetBytes(value));
    }

    public void Vector(Vector3 value)
    {
        Float(value.x);
        Float(value.y);
        Float(value.z);
    }

    public void Color(Color32 value) => Int(value.rgba);

    public void Id(SteamId value) => Marshal.WriteInt64(mem, Inc(8), Ulong2long(value));

    public void Enum<T>(T value) where T : Enum => Byte(Convert.ToByte(value));

    #endregion
}
