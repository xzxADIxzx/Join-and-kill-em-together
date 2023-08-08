namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Net.EntityTypes;

[HarmonyPatch(typeof(EnemyInfoPage), "Start")]
public class DatabasePatch
{
    static string description =
@"After the final war, a third model was hastily created to rebuild the destroyed cities. To save
resources, most of which were spent to the war, this model was based on V1 and produced in the same factories, so it uses the
same exterior plating as V1 and can refuel through contact with blood.

To speed up the rebuilding, they were equipped with a grappling hook and powerful artificial intelligence with a clear bias towards the cooperative, which
communicates with the rest of the instances of this model via internal channels to increase productivity and coherence.

Due to the fact that the V3 model was not originally created for military purposes, it may seem that this is not a very formidable enemy, but they also know about it,
therefore, unlike other machines, they constantly walk in pairs and sometimes in whole flocks.";

    static string strategy =
@"- They are extremely chaotic and it is better to stay away from them, but if you get involved in a battle, then try to act as unpredictably as they are.

- They use the same plating as the first model, so they can also heal with blood. It is recommended to keep a distance from them to prevent their healing.

- Sometimes they will come close to each other to share blood and repair at a safe distance from enemies. At this moment, they become easy targets.

- The best tactic is to take them out one by one, as this way you will reduce the damage taken from them as quickly as possible.";

    static void Prefix(ref SpawnableObjectsDatabase ___objects)
    {
        // there is no point in adding V3 twice
        if (System.Array.Exists(___objects.enemies, obj => obj.identifier == "jaket.v3")) return;

        // for some reason, if you create a prefab after a scriptable object, the second one will self-destruct
        var preview = RemotePlayer.Prefab();
        var v3 = ScriptableObject.CreateInstance<SpawnableObject>();

        // set up all sorts of things
        v3.identifier = "jaket.v3";
        v3.enemyType = EnemyType.Filth;

        v3.backgroundColor = ___objects.enemies[11].backgroundColor;
        v3.gridIcon = ___objects.enemies[11].gridIcon;

        v3.objectName = "V3";
        v3.type = "SUPREME MACHINE";
        v3.description = description;
        v3.strategy = strategy;
        v3.preview = preview;

        // add V3 to the end of the list
        ___objects.enemies = ___objects.enemies.AddToArray(v3);
    }
}
