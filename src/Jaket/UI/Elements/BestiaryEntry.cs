namespace Jaket.UI.Elements;

using System;
using System.Collections.Generic;
using UnityEngine;

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
}
