namespace Jaket.World;

using HarmonyLib;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI;

/// <summary> Class responsible for Cyber Grind synchronization. </summary>
public class CyberGrind : MonoSingleton<CyberGrind> // TODO a lot of work
{
    /// <summary> Text used for displaying current wave number. </summary>
    public Text WaveNumberTextInstance;
    /// <summary> Text used for displaying current enemies left. </summary>
    public Text EnemiesLeftTextInstance;

    /// <summary> Current wave count used for sync. </summary>
    public int CurrentWave;
    /// <summary> How many times pattern is loaded. Can't be bigger than 1. </summary>
    public int LoadTimes; // TODO replace with boolean

    /// <summary> Current pattern used for sync. </summary>
    public ArenaPattern CurrentPattern;
    public int LoadCount;

    /// <summary> Creates a singleton of cyber grind sync tool. </summary>
    public static void Load()
    {
        // initialize the singleton
        UI.Object("Cyber Grind").AddComponent<CyberGrind>();
    }

    /// <summary> Sends the current arena pattern to all clients. </summary>
    /// <param name="pattern"> Pattern to send to clients. </param>
    public void SendPattern(ArenaPattern pattern) => Networking.Send(PacketType.CyberGrindAction, w =>
    {
        w.Int(EndlessGrid.Instance.currentWave);
        w.String(pattern.heights);
        w.String(pattern.prefabs);
    }, size: 4096); // the pattern size is always different, but IO+Networking will send the required size, so we feel free to allocate with a margin

    /// <summary> Reads and loads a pattern from memory. </summary>
    public void LoadPattern(Reader r)
    {
        // set current wave to synced one
        CurrentWave = r.Int();
        // sets current pattern to give it to LoadPattern method of original class
        CurrentPattern = new ArenaPattern { heights = r.String(), prefabs = r.String() };
        LoadPattern(CurrentPattern);
    }

    /// <summary> Loads the given pattern and invokes the next wave. </summary>
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

    /// <summary> Loads the current pattern. </summary>
    public void LoadCurrentPattern() => LoadPattern(CurrentPattern);
}
