namespace Jaket;

using plog;
using System;
using System.Collections.Generic;
using System.IO;

using Jaket.Net;

/// <summary> Custom logger used only by the mod for convenience. </summary>
public class Log
{
    /// <summary> Time format used by this logger. </summary>
    public const string TIME_FORMAT = "yyyy.MM.dd HH:mm:ss";
    /// <summary> Formatted regional time. </summary>
    public static string Time => DateTime.Now.ToString(TIME_FORMAT);

    /// <summary> Number of logs that will be stored in memory before being written. </summary>
    public const int STORAGE_CAPACITY = 32;
    /// <summary> Logs waiting their turn to be written. </summary>
    public static List<string> ToWrite = new();

    /// <summary> Output point for Unity and in-game console. </summary>
    public static Logger Logger;
    /// <summary> Output point for long-term logging. </summary>
    public static string LogPath;

    /// <summary> Creates output points for logs and subscribes to some events. </summary>
    public static void Load()
    {
        Logger = new("Jaket");
        LogPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Location), "logs", $"Log {Time.Replace(':', '.')}.txt");

        Events.OnLobbyAction += () =>
        {
            var lobby = LobbyController.Offline ? "null" : $"{LobbyController.Lobby?.GetData("name")} ({LobbyController.Lobby.Value.Id})";
            var owner = LobbyController.Lobby?.Owner.ToString() ?? "null";
            Debug($"Lobby status updated: cl is {lobby}, owner is {owner}");
        };
        Events.OnLobbyEntered += () => Debug("Entered the new lobby");
    }

    /// <summary> Formats and writes the msg to all output points. </summary>
    public static void LogLevel(Level level, string msg)
    {
        Logger.Log(level == Level.Debug ? $"<color=#BBBBBB>{msg}</color>" : msg, level switch
        {
            Level.Debug or Level.Info => plog.Models.Level.Info,
            Level.Warning => plog.Models.Level.Warning,
            Level.Error or _ => plog.Models.Level.Error,
        });

        ToWrite.Add($"[{Time}] [{new[] { 'D', 'I', 'W', 'E' }[(int)level]}] {msg}");
        if (ToWrite.Count > STORAGE_CAPACITY) Flush();
    }

    /// <summary> Flushes all logs to a file; creates a folder if it doesn't exist. </summary>
    public static void Flush()
    {
        if (ToWrite.Count == 0) return;

        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)); // ensure that the folder is exists
        File.AppendAllLines(LogPath, ToWrite);

        ToWrite.Clear();
    }

    public static void Debug(string msg) => LogLevel(Level.Debug, msg);

    public static void Info(string msg) => LogLevel(Level.Info, msg);

    public static void Warning(string msg) => LogLevel(Level.Warning, msg);

    public static void Error(string msg) => LogLevel(Level.Error, msg);

    public static void Error(Exception ex) => LogLevel(Level.Error, $"{ex.ToString()}\nOuter:\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

    /// <summary> Log importance levels. </summary>
    public enum Level
    {
        Debug, Info, Warning, Error
    }
}
