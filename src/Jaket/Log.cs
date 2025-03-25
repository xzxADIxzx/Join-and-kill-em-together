namespace Jaket;

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Logger = plog.Logger;
using PL = plog.Models.Level;

/// <summary> Logger used by the mod for convenience. </summary>
public class Log
{
    /// <summary> Time format used by the logger. </summary>
    public const string TIME_FORMAT = "yyyy.MM.dd HH:mm:ss";
    /// <summary> Formatted regional time. </summary>
    public static string Time => DateTime.Now.ToString(TIME_FORMAT);

    /// <summary> Number of logs that are stored in memory before being written. </summary>
    public const int STORAGE_CAPACITY = 64;
    /// <summary> Logs waiting their turn to be written. </summary>
    public static List<string> ToWrite = new(STORAGE_CAPACITY);

    /// <summary> Output point for Unity and in-game console. </summary>
    public static Logger Logger;
    /// <summary> Output point for long-term logging. </summary>
    public static string LogPath;

    /// <summary> Creates output points for logs. </summary>
    public static void Load()
    {
        Logger = new("Jaket");
        LogPath = Path.Combine(Plugin.Instance.Location, "logs", $"Log {Time.Replace(':', '.')}.txt");
    }

    /// <summary> Formats and writes the message to all output points. </summary>
    public static void LogLevel(Level level, string msg)
    {
        Logger.Record(level == Level.Debug ? $"<color=#BBBBBB>{msg}</color>" : msg, level switch
        {
            Level.Debug   => PL.Info,
            Level.Info    => PL.Info,
            Level.Warning => PL.Warning,
            Level.Error   => PL.Error,
            _             => PL.Off,
        });

        ToWrite.Add($"[{Time}] [{(char)level}] {msg}");
        if (ToWrite.Count > STORAGE_CAPACITY) Flush();
    }

    /// <summary> Flushes all logs to a file; creates a folder if it doesn't exist. </summary>
    public static void Flush()
    {
        if (ToWrite.Count == 0) return;

        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        File.AppendAllLines(LogPath, ToWrite);

        ToWrite.Clear();
    }

    public static void Debug(string msg) => LogLevel(Level.Debug, msg);

    public static void Info(string msg) => LogLevel(Level.Info, msg);

    public static void Warning(string msg) => LogLevel(Level.Warning, msg);

    public static void Error(string msg) => LogLevel(Level.Error, msg);

    public static void Error(Exception ex) => LogLevel(Level.Error, $"{ex}\nOuter:\n{StackTraceUtility.ExtractStackTrace()}");

    /// <summary> Log importance levels. </summary>
    public enum Level
    {
        Debug   = 'D',
        Info    = 'I',
        Warning = 'W',
        Error   = 'E'
    }
}
