namespace Jaket.IO;

using System.Diagnostics;
using System.Threading;

using Jaket.Net;

/// <summary> Set of different tools for collecting and analyzing various data. </summary>
public static class Stats
{
    /// <summary> Number of subticks accumulated. </summary>
    public static int Subticks;
    /// <summary> Number of bytes received. </summary>
    public static int Received;
    /// <summary> Number of bytes sent. </summary>
    public static int Sent;

    /// <summary> Time spent reading and writing. </summary>
    public static long Read, Write;
    /// <summary> Time spent by the entities and other components. </summary>
    public static long Entity, Common;
    /// <summary> Time spent by the network thread and its jitter. </summary>
    public static long Thread, Jitter;

    /// <summary> Measures the execution time of the given action. </summary>
    public static void Measure(ref long store, Runnable action)
    {
        long s = Stopwatch.GetTimestamp();
        action();
        store += Stopwatch.GetTimestamp() - s;
    }

    /// <summary> Returns the number of milliseconds in a storage. </summary>
    public static float Millis(long store) => store * 1000f / Stopwatch.Frequency;

    /// <summary> Increases sent bytes counter. </summary>
    public static void Add(int bytesCount) => Interlocked.Add(ref Sent, bytesCount);

    /// <summary> Resets accumulated subticks. </summary>
    public static void Reset()
    {
        if (Subticks++ < Networking.TICKS_PER_SECOND * Networking.SUBTICKS_PER_TICK) return;

        Subticks = 0;
        Received = 0;
        Interlocked.Exchange(ref Sent, 0);

        Read = Write = Entity = Common = Thread = Jitter = 0L;
    }
}
