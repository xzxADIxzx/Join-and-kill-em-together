namespace Jaket.IO;

using Steamworks;
using System;
using System.IO;
using UnityEngine;

/// <summary> Builtin binary reader but with some extra methods for convenience. </summary>
public class Reader
{
    /// <summary> Input for reader. </summary>
    private BinaryReader r;

    /// <summary> Creates a reader with the given input. </summary>
    private Reader(BinaryReader r) => this.r = r;

    /// <summary> Current cursor position in the stream. </summary>
    public long Position { get => r.BaseStream.Position; set => r.BaseStream.Position = value; }

    /// <summary> Stream length. </summary>
    public long Length { get => r.BaseStream.Length; }

    /// <summary> Reads data from the given byte array via reader. </summary>
    public static void Read(byte[] data, Action<Reader> cons)
    {
        MemoryStream stream = new(data);
        using (var r = new BinaryReader(stream)) cons(new Reader(r));
    }

    /// <summary> Reads a boolean. </summary>
    public bool Bool() => r.ReadBoolean();

    /// <summary> Reads a byte. </summary>
    public byte Byte() => r.ReadByte();

    /// <summary> Reads bytes. </summary>
    public byte[] Bytes(int count) => r.ReadBytes(count);

    /// <summary> Reads a short. </summary>
    public short Short() => r.ReadInt16();

    /// <summary> Reads an integer. </summary>
    public int Int() => r.ReadInt32();

    /// <summary> Reads a float. </summary>
    public float Float() => r.ReadSingle();

    /// <summary> Reads a string. </summary>
    public string String() => r.ReadString();

    /// <summary> Reads a vector. </summary>
    public Vector3 Vector() => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

    /// <summary> Reads a color. </summary>
    public Color32 Color() => new(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());

    /// <summary> Reads a SteamID. </summary>
    public SteamId Id() => r.ReadUInt64();
}