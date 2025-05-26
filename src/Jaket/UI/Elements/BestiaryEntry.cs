namespace Jaket.UI.Elements;

using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;

/// <summary> Additional entry to insert into the bestiary. </summary>
[Serializable]
public struct BestiaryEntry
{
    /// <summary> List of all entries to insert. </summary>
    public static List<BestiaryEntry> Entries = new();

    /// <summary> Fields to be displayed in the shop terminal. </summary>
    public string name, type, description, strategy;
    /// <summary> Bestiary index to insert the entry at. </summary>
    public int InsertIndex;

    /// <summary> Loads an entry from the given json file and adds it to the insert queue. </summary>
    public static void Load(string entry, int index) => Entries.Add(JsonUtility.FromJson<BestiaryEntry>(entry) with { InsertIndex = index });

    #region harmony

    [HarmonyPatch(typeof(EnemyInfoPage), nameof(EnemyInfoPage.UpdateInfo))]
    [HarmonyPrefix]
    static void Inject(ref SpawnableObjectsDatabase ___objects)
    {
        // the database is either already patched or not vanilla at all
        if (!___objects.enemies.All(e => e.identifier.StartsWith("ultrakill"))) return;

        // there is only one entry at the moment, but in the future...
        foreach (var e in Entries)
        {
            var entry = ScriptableObject.CreateInstance<SpawnableObject>();

            entry.identifier = $"jaket.{e.name}";
            entry.enemyType = EnemyType.Filth;

            entry.backgroundColor = ___objects.enemies[e.InsertIndex].backgroundColor;
            entry.gridIcon    = ModAssets.BestiaryIcon;
            entry.preview     = ModAssets.DollPreview;

            entry.objectName  = e.name;
            entry.type        = e.type;
            entry.description = e.description;
            entry.strategy    = e.strategy;

            Insert(ref ___objects.enemies, e.InsertIndex, entry);
        }
    }

    #endregion
}
