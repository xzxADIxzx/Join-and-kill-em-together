namespace Jaket.IO;

using System;
using System.Runtime.InteropServices;

/// <summary> List of pointers to memory allocated for writers. </summary>
public class Pointers
{
    /// <summary> Amount of bytes that will be reserved. Only 8kb are used most of the time, but sprays occupy up to 256kb. </summary>
    public const int RESERVED = 256 * 1024;

    /// <summary> Pointer to the beginning of the memory reserved for writing data. </summary>
    private static IntPtr pointer;
    /// <summary> Amount of bytes allocated for writers. </summary>
    private static int offset;

    /// <summary> Reserves 8kb for future allocation. </summary>
    public static void Allocate() => pointer = Marshal.AllocHGlobal(RESERVED);

    /// <summary> Allocates the given amount of bytes on the reserved memory. </summary>
    public static IntPtr Allocate(int bytes)
    {
        var alloc = pointer + offset;

        if ((offset += bytes) >= RESERVED) throw new OutOfMemoryException("Attempt to allocate more bytes than were reserved in memory.");
        return alloc;
    }

    /// <summary> Frees the memory allocated for writers. </summary>
    public static void Free() => offset = 0;
}
