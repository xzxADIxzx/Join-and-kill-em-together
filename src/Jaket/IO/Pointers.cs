namespace Jaket.IO;

using System;
using System.Runtime.InteropServices;

/// <summary> Thread-safe allocator of the unmanaged memory. </summary>
public static class Pointers
{
    /// <summary> Number of bytes allocated for each thread. </summary>
    public const int RESERVED = 1024;

    [ThreadStatic]
    private static Ptr memory;

    /// <summary> Allocates and stores a fragment of memory. </summary>
    public static Ptr Allocate() => memory != default ? memory : memory = Marshal.AllocHGlobal(RESERVED);
}
