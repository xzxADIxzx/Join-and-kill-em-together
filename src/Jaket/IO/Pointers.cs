namespace Jaket.IO;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary> List of pointers to memory allocated for writers. </summary>
public class Pointers
{
    private static List<IntPtr> pointers = new();

    /// <summary> Adds a new pointer to the list. </summary>
    public static void Add(IntPtr pointer) => pointers.Add(pointer);

    /// <summary> Frees memory allocated for writers. </summary>
    public static void Free()
    {
        pointers.ForEach(Marshal.FreeHGlobal);
        pointers.Clear();
    }
}
