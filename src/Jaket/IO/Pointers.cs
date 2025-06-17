namespace Jaket.IO;

using System;
using System.Runtime.InteropServices;

/// <summary> Set of different tools for working with the unmanaged memory. </summary>
public static class Pointers
{
    /// <summary> Number of bytes to allocate for the project needs. </summary>
    public const int RESERVED = 1024 * 1024;

    /// <summary> Pointer to the beginning of the allocated memory. </summary>
    private static Ptr memory;
    /// <summary> Number of bytes reserved in the allocated memory. </summary>
    private static int offset;

    /// <summary> Allocates a portion of memory for future reservation. </summary>
    public static void Allocate() => memory = Marshal.AllocHGlobal(RESERVED);

    /// <summary> Reserves the given number of bytes in the memory. </summary>
    public static Ptr Reserve(int bytesCount)
    {
        offset += bytesCount;

        if (offset >= RESERVED) throw new OutOfMemoryException("Caught an attempt to reserve more bytes than were allocated in the unmanaged memory");

        return memory + offset - bytesCount;
    }

    /// <summary> Frees up the reserved memory. </summary>
    public static void Free() => offset = 0;
}
