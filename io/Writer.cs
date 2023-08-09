namespace Jaket.IO;

using Steamworks;
using System;
using System.IO;
using UnityEngine;

/// <summary> Builtin binary writer but with some extra methods for convenience. </summary>
public class Writer
{
    /// <summary> Output for writer. </summary>
    private BinaryWriter w;

    /// <summary> Creates a writer with the given output. </summary>
    private Writer(BinaryWriter w) => this.w = w;

    /// <summary> Current cursor position in the stream. </summary>
    public long Position { get => w.BaseStream.Position; set => w.BaseStream.Position = value; }

    /// <summary> Stream length. </summary>
    public long Length { get => w.BaseStream.Length; }

    /// <summary> Writes data to return byte array via writer. </summary>
    public static byte[] Write(Action<Writer> cons)
    {
        MemoryStream stream = new();
        using (var w = new BinaryWriter(stream)) cons(new Writer(w));

        return stream.ToArray();
    }

    /// <summary> Writes a boolean. </summary>
    public void Bool(bool value) => w.Write(value);

    /// <summary> Writes a byte. </summary>
    public void Byte(byte value) => w.Write(value);

    /// <summary> Writes bytes. </summary>
    public void Bytes(byte[] value) => w.Write(value);

    /// <summary> Writes a short. </summary>
    public void Short(short value) => w.Write(value);

    /// <summary> Writes an integer. </summary>
    public void Int(int value) => w.Write(value);

    /// <summary> Writes a float. </summary>
    public void Float(float value) => w.Write(value);

    /// <summary> Writes a string. </summary>
    public void String(string value) => w.Write(value);

    /// <summary> Writes a vector. </summary>
    public void Vector(Vector3 value)
    {
        w.Write(value.x);
        w.Write(value.y);
        w.Write(value.z);
    }

    /// <summary> Writes a color. </summary>
    public void Color(Color32 color) => w.Write(color.rgba);

    /// <summary> Writes a SteamID. </summary>
    public void Id(SteamId value) => w.Write(value);
}