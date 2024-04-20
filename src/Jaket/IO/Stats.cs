namespace Jaket.IO;

using System;
using System.Diagnostics;

/// <summary> Class that collects reading/writting data statistics for subsequent analysis and optimization of traffic. </summary>
public class Stats
{
    /// <summary> Maximum values achieved throughout the game. </summary>
    public static int PeakWrite, PeakRead;
    /// <summary> Values of the last second aka bytes per second. </summary>
    public static int LastWrite, LastRead;
    /// <summary> Values of the current second. </summary>
    public static int Write, Read;

    /// <summary> Maximum time achieved throughout the game. </summary>
    public static float PeakWriteTime, PeakReadTime;
    /// <summary> Time that has gone to read and write in milliseconds. </summary>
    public static float WriteTime, ReadTime;

    /// <summary> Begins to record statistics. </summary>
    public static void StartRecord() => Events.EverySecond += () =>
    {
        LastWrite = Write; LastRead = Read;

        if (Write > PeakWrite) PeakWrite = Write;
        if (Read > PeakRead) PeakRead = Read;

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

        if (WriteTime > PeakWriteTime) PeakWriteTime = WriteTime;
        if (ReadTime > PeakReadTime) PeakReadTime = ReadTime;
    }
}
