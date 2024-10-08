namespace Jaket.Patches;

using HarmonyLib;
using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

[HarmonyPatch(typeof(EnemyInfoPage), "Start")]
public class BestiaryPatch
{
    static void Prefix(ref SpawnableObjectsDatabase ___objects)
    {
        // there is no point in adding V3 twice
        if (Array.Exists(___objects.enemies, obj => obj.identifier == "jaket.v3")) return;
        if (Array.Exists(___objects.enemies, obj => obj.identifier == "jaket.v4")) return;

        var v3Entry = BestiaryEntry.Load("V3");
        BestiaryEntry.Insert(
            ref ___objects,
            index: 15,
            preview: DollAssets.Preview,
            icon: DollAssets.Icons[0],
            
            id: "jaket.v3",
            entry: v3Entry
        );

        var v4Entry = BestiaryEntry.Load("V4");
        BestiaryEntry.Insert(
            ref ___objects,
            index: 16,
            preview: DollAssets.CreatePreviewWithSkin(
                DollAssets.WingTextures[(int)Team.White],
                DollAssets.BodyTextures[(int)Team.White]
            ),
            icon: DollAssets.Icons[1],

            id: "YetAnotherJaketFork.v4",
            entry: v4Entry
        );

        var f1 = ScriptableObject.CreateInstance<SpawnableObject>();

        BestiaryEntry.Insert(
            ref ___objects,
            index: 17,

            preview: DollAssets.CreatePreviewWithSkin(
                DollAssets.WingTextures[(int)Team.Fraud],
                DollAssets.BodyTextures[(int)Team.Fraud]
            ),
            icon: DollAssets.Icons[2],

            id: "YetAnotherJaketFork.f1",
            name: "F1",
            desc: "Created by Idlecreeper"
        );
    }
}

[Serializable]
public class BestiaryEntry
{
    /// <summary> Bestiary entry fields displayed in terminal. </summary>
    public string name, type, description, strategy;
    /// <summary> Loads the V3 bestiary entry from the bundle. </summary>
    public static BestiaryEntry Load(string name) 
    {
        return JsonUtility.FromJson<BestiaryEntry>(DollAssets.Bundle.LoadAsset<TextAsset>(name + "-bestiary-entry").text);
    }

    public static void Insert(ref SpawnableObjectsDatabase bestiaryEntries, int index, GameObject preview, Sprite icon, string id, string name = "", string type = "SUPREME MACHINE", string desc = "", string strat = "")
    {
        var spawnable = ScriptableObject.CreateInstance<SpawnableObject>();

        // set up all sorts of things
        spawnable.identifier = id;
        spawnable.enemyType = EnemyType.Filth;

        spawnable.backgroundColor = bestiaryEntries.enemies[11].backgroundColor;
        spawnable.gridIcon = icon;

        spawnable.objectName = name;
        spawnable.type = type;
        spawnable.description = desc;
        spawnable.strategy = strat;
        spawnable.preview = preview;

        // insert v4 after v3 in the list
        Array.Resize(ref bestiaryEntries.enemies, bestiaryEntries.enemies.Length + 1);
        Array.Copy(bestiaryEntries.enemies, index, bestiaryEntries.enemies, index + 1, bestiaryEntries.enemies.Length - (index + 1));
        bestiaryEntries.enemies[index] = spawnable;
    }

    public static void Insert(ref SpawnableObjectsDatabase bestiaryEntries, int index, GameObject preview, Sprite icon, string id, BestiaryEntry entry) =>
        Insert(ref bestiaryEntries, index, preview, icon, id, entry.name, entry.type, entry.description, entry.strategy);
}
