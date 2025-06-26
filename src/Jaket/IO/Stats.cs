namespace Jaket.IO;

using System.Diagnostics;

using Jaket.Net;
using Jaket.UI;

/// <summary> Set of different tools for collecting various data for subsequent analysis and optimization. </summary>
public static class Stats
{
    /// <summary> Number of subticks accumulated in the current statistics frame. </summary>
    public static int Subticks;
    /// <summary> Number of bytes read and written. </summary>
    public static int ReadBs, WriteBs;

    /// <summary> Time spent reading and writing in milliseconds. </summary>
    public static float ReadMs, WriteMs;
    /// <summary> Time spent updating entities and targets in milliseconds. </summary>
    public static float EntityMs, TargetMs;
    /// <summary> Time spent flushing data and total time in milliseconds. </summary>
    public static float TotalMs, FlushMs;

    /// <summary> Timer used to measure the time of performing various actions. </summary>
    private static Stopwatch sw = new();

    /// <summary> Starts recording various data. </summary>
    public static void StartRecord() => Events.EveryTick += () =>
    {
        if (++Subticks % (Networking.TICKS_PER_SECOND * Networking.SUBTICKS_PER_TICK) != 0) return;
        Subticks = 0;

        // count the total time spent
        TotalMs = ReadMs + WriteMs + FlushMs + EntityMs + TargetMs;

        // flush the current frame to the debug fragment
        if (Version.DEBUG || UI.Debug.Shown) UI.Debug.Rebuild();

        ReadBs = WriteBs = 0;
        ReadMs = WriteMs = EntityMs = TargetMs = TotalMs = FlushMs = 0f;
    };

    /// <summary> Measures the time of execution of the given action. </summary>
    public static void MeasureTime(ref float store, Runnable action)
    {
        sw.Restart();
        action();
        store += sw.ElapsedTicks / 10000f;
    }
}
