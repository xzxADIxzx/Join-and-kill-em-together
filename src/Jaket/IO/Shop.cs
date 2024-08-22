namespace Jaket.IO;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Jaket.Assets;

/// <summary> Class responsible for saving purchases in the mod shop. </summary>
public class Shop
{
    /// <summary> Path to the file in which the purchases are saved. </summary>
    public static string SavePath => Path.Combine(GameProgressSaver.SavePath, "customization.bepis");
    /// <summary> The first jacket is approximately in the middle of the list, but not exactly, because there are two more hats. </summary>
    public static int FirstJacket => Entries.Length / 2 + 1;

    /// <summary> Cosmetic trinkets that can be bought in the terminal shop. </summary>
    public static ShopEntry[] Entries;

    /// <summary> Hat and jacket chosen by the player. </summary>
    public static int SelectedHat, SelectedJacket;
    /// <summary> Bit mask containing purchased trinkets. </summary>
    public static ulong Unlocked;

    /// <summary> Loads the list of shop entries. </summary>
    public static void Load(string json)
    {
        json = Regex.Replace(json, "//.*?\n|\\[\n", string.Empty);
        Entries = new ShopEntry[json.Count(c => c == '\n') / 4];

        for (int i = 0, s = 0, e = 0; i < Entries.Length; i++)
        {
            s = json.IndexOf('{', e);
            e = json.IndexOf('}', s);

            Entries[i] = JsonUtility.FromJson<ShopEntry>(json.Substring(s, e - s + 1));
        }
    }

    #region save & load

    /// <summary> Saves the purchases to this save file. </summary>
    public static void SavePurchases()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);

        using var fs = File.OpenWrite(SavePath);
        BinaryWriter bw = new(fs);

        bw.Write(SelectedHat);
        bw.Write(SelectedJacket);
        bw.Write(Unlocked);
    }

    /// <summary> Loads the purchases made in this save file. </summary>
    public static void LoadPurchases()
    {
        SelectedHat = 0;
        SelectedJacket = FirstJacket;
        Unlocked = 0L;

        if (File.Exists(SavePath))
        {
            using var fs = File.OpenRead(SavePath);
            BinaryReader br = new(fs);

            SelectedHat = br.ReadInt32();
            SelectedJacket = br.ReadInt32();
            Unlocked = br.ReadUInt64();
        }
    }

    #endregion
    #region progress

    /// <summary> Whether the given entry was purchased in this save. </summary>
    public static bool IsUnlocked(int entryId) => Entries[entryId].cost == 0 || (Unlocked & 1U << entryId) == 1U << entryId;

    /// <summary> Purchases the given entry. </summary>
    public static void Unlock(int entryId) => Unlocked |= 1U << entryId;

    /// <summary> Returns the icon of the given entry. </summary>
    public static Sprite Icon(int entryId) => ModAssets.ShopIcons[Entries[entryId].historicalId];

    #endregion
}

[Serializable]
public class ShopEntry
{
    /// <summary> This id is assigned to a new entry once and never changes, so that purchases are not discarded after adding another entry. </summary>
    public int historicalId;
    /// <summary> This id may change and displays the position of purchase in the suits hierarchy. </summary>
    public int hierarchyId;
    /// <summary> Cost of the entry in Ps. </summary>
    public int cost;
}
