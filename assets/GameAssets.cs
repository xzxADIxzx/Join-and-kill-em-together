namespace Jaket.Assets;

using UnityEngine;

/// <summary> Class that works with the assets of the game. </summary>
public class GameAssets
{
    /// <summary> List of names of all enemies. </summary>
    public static readonly string[] Enemies = new[]
    {
        "Zombie", "Projectile Zombie", "Super Projectile Zombie", "ShotgunHusk", "MinosBoss", "Stalker",
        "Sisyphus", "Ferryman", "SwordsMachineNonboss", "Drone", "Streetcleaner", "V2",
        "Mindflayer", "V2 Green Arm Variant", "Turret", "Spider", "StatueEnemy", "Mass",
        "Idol", "Gabriel", "Virtue", "Gabriel 2nd Variant", "Wicked", "Cancerous Rodent",
        "Very Cancerous Rodent", "Mandalore", "Flesh Prison", "DroneFlesh", "Flesh Prison 2", "DroneFleshCamera Variant",
        "DroneSkull Variant", "MinosPrime", "SisyphusPrime",
    };

    /// <summary> Loads an enemy prefab by name. </summary>
    public static GameObject Enemy(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/Enemies/{name}.prefab");
    /// <summary> Loads an item prefab by name. </summary>
    public static GameObject Item(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/Items/{name}.prefab");
    /// <summary> Loads a dev plushy prefab by name. </summary>
    public static GameObject Plushy(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/Items/DevPlushies/{name}.prefab");
}
