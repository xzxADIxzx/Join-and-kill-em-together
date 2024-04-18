namespace Jaket.IO;

/// <summary> Class that collects reading/writting data statistics for subsequent analysis and optimization of traffic. </summary>
public class Stats
{
    /// <summary> Maximum values achieved throughout the game. </summary>
    public static int PeakWrite, PeakRead;
    /// <summary> Values of the last second aka bytes per second. </summary>
    public static int LastWrite, LastRead;
    /// <summary> Values of the current second. </summary>
    public static int Write, Read;

    /// <summary> Begins to record statistics. </summary>
    public static void StartRecord() => Events.EverySecond += () =>
    {
        LastWrite = Write; LastRead = Read;

        if (Write > PeakWrite) PeakWrite = Write;
        if (Read > PeakRead) PeakRead = Read;
    };
}
