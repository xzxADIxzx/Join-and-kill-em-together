namespace Jaket.World;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class responsible for Cyber Grind synchronization. </summary>
public class CyberGrind
{
    private static EndlessGrid grid => EndlessGrid.Instance;
    private static NewMovement nm => NewMovement.Instance;

    /// <summary> Current wave number used for display on the Huge Flying Panel™. </summary>
    public static int CurrentWave;
    /// <summary> Current pattern containing heights and prefabs. </summary>
    public static ArenaPattern CurrentPattern;

    /// <summary> Sends the current arena pattern to all clients. </summary>
    public static void SyncPattern(ArenaPattern pattern) => Networking.Send(PacketType.CyberGrindAction, w =>
    {
        w.Int(grid.currentWave);
        w.String(pattern.heights);
        w.String(pattern.prefabs);
    }, size: 4096); // the pattern size is always different, but IO+Networking will send the required size, so we feel free to allocate with a margin

    /// <summary> Reads and loads a pattern from memory. </summary>
    public static void LoadPattern(Reader r)
    {
        // read the wave and activate the Huge Flying Panel™
        CurrentWave = r.Int();
        grid.waveNumberText.transform.parent.parent.gameObject.SetActive(true);

        // read the pattern and launch the wave
        CurrentPattern = new ArenaPattern { heights = r.String(), prefabs = r.String() };
        LoadPattern(CurrentPattern);
    }

    /// <summary> Loads the given pattern and invokes the next wave. </summary>
    public static void LoadPattern(ArenaPattern pattern)
    {
        // start a new wave with the synced pattern
        AccessTools.Method(typeof(EndlessGrid), "NextWave").Invoke(grid, new object[] { });

        // do not reset any value if it is the first load
        var col = grid.GetComponent<Collider>();
        if (col.enabled) { col.enabled = false; return; }

        // play cheering sound effect
        var cr = CrowdReactions.Instance;
        cr.React(cr.cheerLong);

        if (nm.hp > 0)
        {
            WeaponCharges.Instance.MaxCharges();

            nm.ResetHardDamage();
            nm.exploded = false;
            nm.GetHealth(999, silent: true);
            nm.FullStamina();
        }
    }
}
