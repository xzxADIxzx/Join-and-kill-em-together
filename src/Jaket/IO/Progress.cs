namespace Jaket.IO;

/// <summary> Storage responsible for general progress on missions. </summary>
public static class Progress
{
    /// <summary> Storage itself. </summary>
    private static byte[] ranks;

    /// <summary> Saves the rank of the present mission. </summary>
    public static void Save()
    {
        Log.Info("[SAVE] Saving the rank...");

        var pf = PrefsManager.Instance;
        var sm = StatsManager.Instance;

        if (SceneHelper.IsPlayingCustom)
        {
            Log.Info("[SAVE] Skipping due to the mission being custom");
            return;
        }
        if (GetMissionName.EnumerateMissionNumbers().All(n => n != sm.levelNumber))
        {
            Log.Info("[SAVE] Skipping due to the mission being invalid");
            return;
        }
        if (pf.GetInt("difficulty") < 0 || pf.GetInt("difficulty") > 5)
        {
            Log.Info("[SAVE] Skipping due to the difficulty being invalid");
            return;
        }

        if (ranks == null) Load();

        Save
        (
            sm.levelNumber - (sm.levelNumber >= 666 ? 621 : sm.levelNumber >= 100 ? 65 : 1),
            pf.GetInt("difficulty"),
            (byte)UnityEngine.Mathf.Clamp(sm.rankScore + 1, 0, 6)
        );
    }

    /// <summary> Saves the rank of the certain mission. </summary>
    public static void Save(int mission, int difficulty, byte rank)
    {
        if (ranks[mission * 6 + difficulty] < rank)
        {
            ranks[mission * 6 + difficulty] = rank;

            Files.Write(Files.Progress, w => w.Write(ranks));

            Log.Info($"[SAVE] Saved the rank {Sign(rank)} at {mission:00}/{difficulty}#{mission * 6 + difficulty}");
        }
        else Log.Info("[SAVE] Skipping due to the rank being lower than the saved one");
    }

    /// <summary> Loads all ranks from the file storage. </summary>
    public static void Load()
    {
        if (Files.Exists(Files.Progress))
            Files.Read(Files.Progress, r => ranks = r.ReadBytes(48 * 6));
        else
            ranks = new byte[48 * 6];
    }

    /// <summary> Loads the rank of the certain mission. </summary>
    public static byte Load(int mission, int difficulty)
    {
        if (ranks == null) Load();
        return ranks[mission * 6 + difficulty];
    }

    /// <summary> Returns the sign of the provided rank. </summary>
    public static string Sign(byte rank) => rank switch
    {
        1 => "<color=#0094FF>D</color>",
        2 => "<color=#4CFF00>C</color>",
        3 => "<color=#FFD800>B</color>",
        4 => "<color=#FF6A00>A</color>",
        5 => "<color=#FF0000>S</color>",
        6 => "<color=#FFFFFF>P</color>",
        _ => null,
    };
}
