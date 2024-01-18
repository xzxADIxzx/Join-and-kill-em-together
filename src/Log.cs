namespace Jaket;

using plog;
using System;
using System.IO;

using Jaket.Net;

/// <summary> Custom logger used only by the mod for convenience. </summary>
public class Log
{
    /// <summary> Time format used by this logger. </summary>
    public const string TIME_FORMAT = "yyyy.MM.dd HH:mm:ss";
    /// <summary> Formatted regional time. </summary>
    public static string Time => DateTime.Now.ToString(TIME_FORMAT);

    /// <summary> Output point for Unity and in-game console. </summary>
    public static Logger Logger;
    /// <summary> Output point for long-term logging. </summary>
    public static string LogPath;

    /// <summary> Creates output points for logs and subscribes to some events. </summary>
    public static void Load()
    {
        Logger = new("Jaket");
        LogPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "logs", $"Log {Time.Replace(':', '.')}.txt");

        Events.OnLobbyAction += () =>
        {
            var lobby = LobbyController.Lobby == null ? "null" : $"{LobbyController.Lobby?.GetData("name")} ({LobbyController.Lobby.Value.Id})";
            var owner = LobbyController.Lobby?.Owner.ToString() ?? "null";
            Debug($"Lobby status updated: cl is {lobby}, owner is {owner}");
        };
        Events.OnLobbyEntered += () => Debug("Entered the new lobby");
    }

    /// <summary> Formats and writes the msg to all output points. </summary>
    public static void LogLevel(Level level, string msg)
    {
        Logger.Log(level == Level.Debug ? $"<color=#CCCCCC>{msg}</color>" : msg, level switch
        {
            Level.Debug or Level.Info => plog.Models.Level.Info,
            Level.Warning => plog.Models.Level.Warning,
            Level.Error or _ => plog.Models.Level.Error,
        });
    }

    public static void Debug(string msg) => LogLevel(Level.Debug, msg);

    public static void Info(string msg) => LogLevel(Level.Info, msg);

    public static void Warning(string msg) => LogLevel(Level.Warning, msg);

    public static void Error(string msg) => LogLevel(Level.Error, msg);

    /// <summary> Log importance levels. </summary>
    public enum Level
    {
        Debug, Info, Warning, Error
    }
}
