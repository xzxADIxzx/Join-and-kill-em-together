namespace Jaket.Patches;

using HarmonyLib;
using System;
using UnityEngine;

using Jaket.Assets;

[HarmonyPatch(typeof(EnemyInfoPage), "Start")]
public class BestiaryPatch
{
    static void Prefix(ref SpawnableObjectsDatabase ___objects)
    {
        // there is no point in adding V3 twice
        if (Array.Exists(___objects.enemies, obj => obj.identifier == "jaket.v3")) return;

        var v3 = ScriptableObject.CreateInstance<SpawnableObject>();
        var entry = BestiaryEntry.Load();

        // set up all sorts of things
        v3.identifier = "jaket.v3";
        v3.enemyType = EnemyType.Filth;

        v3.backgroundColor = ___objects.enemies[11].backgroundColor;
        v3.gridIcon = DollAssets.Icon;

        v3.objectName = entry.name;
        v3.type = entry.type;
        v3.description = entry.description;
        v3.strategy = entry.strategy;
        v3.preview = DollAssets.Preview;

        // insert V3 after the turret in the list
        Array.Resize(ref ___objects.enemies, ___objects.enemies.Length + 1);
        Array.Copy(___objects.enemies, 15, ___objects.enemies, 16, ___objects.enemies.Length - 16);
        ___objects.enemies[15] = v3;
    }
}

[Serializable]
public class BestiaryEntry
{
    /// <summary> Bestiary entry fields displayed in terminal. </summary>
    public string name, type, description, strategy;
    /// <summary> Loads the V3 bestiary entry from the bundle. </summary>
    public static BestiaryEntry Load() => JsonUtility.FromJson<BestiaryEntry>(DollAssets.Bundle.LoadAsset<TextAsset>("V3-bestiary-entry").text);
}
