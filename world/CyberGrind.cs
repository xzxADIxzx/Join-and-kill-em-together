namespace Jaket.World;

using HarmonyLib;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI;
using UnityEngine.UI;

/// <summary> Class responsible for Cybergrind synchronization </summary>
public class CyberGrind : MonoSingleton<CyberGrind>
{
    /// <summary> UI Text, used for displaying current wave number. </summary>
    public Text WaveNumberTextInstance;
    /// <summary> UI Text, used for displaying current enemies left. </summary>
    public Text EnemiesLeftTextInstance;
    public DeathZone GridDeathZoneInstance;

    /// <summary> Current wave count used for sync. </summary>
    public int CurrentWave;
    /// <summary> How many times pattern is loaded. Can't be bigger that 1. </summary>
    public int LoadTimes;

    /// <summary> Current pattern used for sync. </summary>
    public ArenaPattern CurrentPattern;
    public int LoadCount;

    /// <summary> Sends the current arena pattern to all clients. </summary>
    /// <param name="pattern"> Pattern to send to clients. </param>
    public void SendPattern(ArenaPattern pattern)
    {
        // serialize pattern to send to clients
        var data = SerializePattern(pattern);
        // sending to clients
        Networking.Redirect(Writer.Write(w =>
        {
            // wave number
            w.Int(EndlessGrid.Instance.currentWave);
            // pattern
            w.String(data);
        }), PacketType.CybergrindAction);
    }

    /// <summary> Load pattern serialized pattern and wave from server. </summary>
    /// <param name="data"> String type represented in <see cref="SerializePattern(ArenaPattern)"/> </param>
    public void LoadPattern(int currentWave, string data)
    {
        // sets current pattern to give it to LoadPattern method of original class
        CurrentPattern = DeserializePattern(data);
        // set current wave to synced one
        CurrentWave = currentWave;
        LoadPattern(CurrentPattern);
    }

    /// <summary> Loads pattern and invoking next wave. </summary>
    /// <param name="pattern"> <see cref="ArenaPattern"/> to load. </param>
    public void LoadPattern(ArenaPattern pattern)
    {
        // sets current pattern to give it to LoadPattern method of original class
        CurrentPattern = pattern;
        // start a new wave with server pattern
        AccessTools.Method(typeof(EndlessGrid), "NextWave").Invoke(EndlessGrid.Instance, new object[] { });

        // Do not make new wave if it is the first time
        if (LoadTimes < 1)
        {
            LoadTimes++;
            return;
        }

        // Resetting weapon charges (for example, railgun will be charged and etc.)
        WeaponCharges.Instance.MaxCharges();

        // play cheering sound effect
        var cr = CrowdReactions.Instance;
        cr.React(cr.cheerLong);

        // resetting values after each wave
        var nmov = NewMovement.Instance;
        if (nmov.hp > 0)
        {
            nmov.ResetHardDamage();
            nmov.exploded = false;
            nmov.GetHealth(999, silent: true);
            nmov.FullStamina();
        }
    }

    /// <summary> Loads current pattern. </summary>
    public void LoadCurrentPattern() => LoadPattern(CurrentPattern);

    /// <summary> Deserialize pattern to load it to client. </summary>
    /// <param name="data"> String to deserialize to <see cref="ArenaPattern"/>. </param>
    public ArenaPattern DeserializePattern(string data)
    {
        // split a pattern into heights and prefabs.
        string[] parts = data.Split('|');
        return new ArenaPattern { heights = parts[0], prefabs = parts[1] };
    }

    /// <summary> Serializes pattern to string to send it to clients. </summary>
    /// <param name="arena"> <see cref="ArenaPattern"/> to serialize to string. </param>
    public string SerializePattern(ArenaPattern arena) => $"{arena.heights}|{arena.prefabs}";

    public static void Load()
    {
        // initialize the singleton
        UI.Object("CyberGrind").AddComponent<CyberGrind>();
    }
}