namespace Jaket.World;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class responsible for Cyber Grind synchronization. </summary>
public class CyberGrind
{
    /// <summary> Current wave number used for display on the Huge Flying Panel™. </summary>
    public static int CurrentWave;
    /// <summary> Current pattern containing heights and prefabs. </summary>
    public static ArenaPattern CurrentPattern;

    /// <summary> Number of living players. </summary>
    public static int PlayersAlive()
    {
        int amount = nm.dead ? 0 : 1;
        Networking.Entities.Player(p => p.Health > 0, p => amount++);
        return amount;
    }

    /// <summary> Sends the current arena pattern to all clients. </summary>
    public static void SyncPattern(ArenaPattern pattern) => Networking.Send(PacketType.CyberAction, 6 + (pattern.heights.Length + pattern.prefabs.Length) * 2, w =>
    {
        // TODO parse pattern to optimize the size of the packet
        w.Int(eg.currentWave);
        w.String(pattern.heights);
        w.String(pattern.prefabs);
    });

    /// <summary> Reads and loads a pattern from memory. </summary>
    public static void LoadPattern(Reader r)
    {
        // read the wave and activate the Huge Flying Panel™
        CurrentWave = r.Int();
        eg.waveNumberText.transform.parent.parent.gameObject.SetActive(true);

        // read the pattern and launch the wave
        CurrentPattern = new ArenaPattern { heights = r.String(), prefabs = r.String() };
        LoadPattern(CurrentPattern);
    }

    /// <summary> Loads the given pattern and invokes the next wave. </summary>
    public static void LoadPattern(ArenaPattern pattern)
    {
        // TODO replace with a patch
        // start a new wave with the synced pattern
        eg.NextWave();

        // do not reset any value if it is the first load
        var col = eg.GetComponent<Collider>();
        if (col.enabled)
        {
            col.enabled = false; // start the timer and the music
            ObjFind("Everything").ObjFind("Timer").SetActive(true);
            return;
        }

        // play cheering sound effect
        cr.React(cr.cheerLong);

        if (nm.hp > 0)
        {
            wc.MaxCharges();
            nm.ResetHardDamage();
            nm.exploded = false;
            nm.GetHealth(999, silent: true);
            nm.FullStamina();
        }
        else Checkpoints.Respawn(new(0f, 80f, 62.5f), 0f, true);
    }
}
