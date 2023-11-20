namespace Jaket.World;

using HarmonyLib;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI;
using System.Text;
using UnityEngine.UI;

/// <summary> Class responsible for Cybergrind synchronization </summary>
public class CyberGrind : MonoSingleton<CyberGrind>
{
    /// <summary> Original CyberGring from the game. </summary>
    public static EndlessGrid EndlessGridInstance;

    /// <summary> UI Text, used for displaying current wave number. </summary>
    public static Text WaveNumberTextInstance;
    /// <summary> UI Text, used for displaying current enemies left. </summary>
    public static Text EnemiesLeftTextInstance;
    public static DeathZone GridDeathZoneInstance;

    /// <summary> Current wave count used for sync. </summary>
    public static int CurrentWave;
    /// <summary> Current pattern used for sync. </summary>
    public static ArenaPattern CurrentPattern;

    /// <summary> Sends the current arena pattern to all clients. </summary>
    /// <param name="pattern"> Pattern to send to clients. </param>
    public static void SendPattern(ArenaPattern pattern)
    {
        // serialize pattern to send to clients
        var data = SerializePattern(pattern);
        // sending to clients
        Networking.Redirect(Writer.Write(w =>
        {
            // wave number
            w.Int(EndlessGridInstance.currentWave);
            // pattern
            w.String(data);
        }), PacketType.CybergrindAction);
    }

    /// <summary> Load pattern serialized pattern and wave from server. </summary>
    /// <param name="data"> String type represented in <see cref="SerializePattern(ArenaPattern)"/> </param>
    public static void LoadPattern(int currentWave, string data)
    {
        // sets current pattern to give it to LoadPattern method of original class
        CurrentPattern = DeserializePattern(data);
        // set current wave to synced one
        CurrentWave = currentWave;
        LoadPattern(CurrentPattern);
    }

    /// <summary> Loads pattern and invoking next wave. </summary>
    /// <param name="pattern"> <see cref="ArenaPattern"/> to load. </param>
    public static void LoadPattern(ArenaPattern pattern)
    {
        // sets current pattern to give it to LoadPattern method of original class
        CurrentPattern = pattern;
        // start a new wave with server pattern
        AccessTools.Method(typeof(EndlessGrid), "NextWave").Invoke(EndlessGridInstance, new object[] { });
    }

    /// <summary> Loads current pattern. </summary>
    public static void LoadCurrentPattern() => LoadPattern(CurrentPattern);

    /// <summary> Deserialize pattern to load it to client. </summary>
    /// <param name="data"> String to deserialize to <see cref="ArenaPattern"/>. </param>
    public static ArenaPattern DeserializePattern(string data)
    {
        // split a pattern into heights and prefabs.
        string[] parts = data.Split('|');
        return new ArenaPattern { heights = parts[0], prefabs = parts[1] };
    }

    /// <summary> Serializes pattern to string to send it to clients. </summary>
    /// <param name="arena"> <see cref="ArenaPattern"/> to serialize to string. </param>
    public static string SerializePattern(ArenaPattern arena) => $"{arena.heights}|{arena.prefabs}";

    public static void Load()
    {
        // initialize the singleton
        UI.Object("CyberGrind").AddComponent<CyberGrind>();
    }
}