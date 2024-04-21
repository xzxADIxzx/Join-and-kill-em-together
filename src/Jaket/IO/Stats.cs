namespace Jaket.IO;

using System;
using System.Diagnostics;
using UnityEngine;

/// <summary> Class that collects reading/writting data statistics for subsequent analysis and optimization of traffic. </summary>
public class Stats
{
    /// <summary> Values of the last second aka bytes per second. </summary>
    public static int LastWrite, LastRead;
    /// <summary> Values of the current second. </summary>
    public static int Write, Read;

    /// <summary> Time that has gone to read and write in milliseconds. </summary>
    public static float WriteTime, ReadTime, DeltaTimeOnRecord;

    /// <summary> Begins to record statistics. </summary>
    public static void StartRecord() => Events.EverySecond += () =>
    {
        LastWrite = Write; LastRead = Read;
        Write = Read = 0;
    };

    /// <summary> Measures the time of execution of the read and write logic. </summary>
    public static void MeasureTime(Action read, Action write)
    {
        var sw = Stopwatch.StartNew();
        read();
        ReadTime = sw.ElapsedTicks / 10000f;

        sw.Restart();
        write();
        WriteTime = sw.ElapsedTicks / 10000f;

        DeltaTimeOnRecord = Time.deltaTime;
    }
}
