namespace Jaket.IO;

using System;
using System.Diagnostics;

/// <summary> Class that collects reading/writing data statistics for subsequent analysis and optimization of traffic. </summary>
public class Stats
{
    /// <summary> Number of read and written bytes per second. </summary>
    public static int Write, LastWrite, Read, LastRead;
    /// <summary> Time that has gone to read and write in milliseconds. </summary>
    public static float WriteTime, LastWriteTime, ReadTime, LastReadTime;
    /// <summary> Time that has gone to update entities logic in milliseconds. </summary>
    public static float TargetUpdate, LastTargetUpdate, EntityUpdate, LastEntityUpdate;

    /// <summary> Timer used to measure the time of performing various actions. </summary>
    private static Stopwatch sw = new();

    /// <summary> Begins to record statistics. </summary>
    public static void StartRecord() => Events.EverySecond += () =>
    {
        LastWrite = Write; LastRead = Read;
        Write = Read = 0;

        LastWriteTime = WriteTime; LastReadTime = ReadTime;
        WriteTime = ReadTime = 0f;

        LastTargetUpdate = TargetUpdate; LastEntityUpdate = EntityUpdate;
        TargetUpdate = EntityUpdate = 0f;
    };

    /// <summary> Measures the time of execution of the given action. </summary>
    public static void MeasureTime(ref float store, Action action)
    {
        sw.Restart();
        action();
        store += sw.ElapsedTicks / 10000f;
    }

    /// <summary> Measures the time of execution of entities logic. </summary>
    public static void MTE(Action action) => MeasureTime(ref EntityUpdate, action);
}
