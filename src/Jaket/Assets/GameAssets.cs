namespace Jaket.Assets;

using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary> Class that works with the assets of the game. </summary>
public class GameAssets
{
    /// <summary> List of items that mustn't be synchronized, because they are not items at all. </summary>
    public static readonly string[] ItemExceptions = new[]
    { "Minotaur", "Tram (3)", "BombTrigger", "BombStationTramTeleporterKey", "Checker" };

    /// <summary> List of internal names of all enemies. </summary>
    public static readonly string[] Enemies = new[]
    {
        "Zombie", "Projectile Zombie", "Super Projectile Zombie", "ShotgunHusk", "MinosBoss", "Stalker", "Sisyphus", "Ferryman",
        "SwordsMachineNonboss", "Drone", "Streetcleaner", "Mindflayer", "V2", "V2 Green Arm Variant", "Turret", "Gutterman",
        "Guttertank", "Spider", "StatueEnemy", "Mass", "Idol", "Mannequin", "Minotaur", "Virtue",
        "Gabriel", "Gabriel 2nd Variant", "Wicked", "Flesh Prison", "DroneFlesh", "Flesh Prison 2", "DroneFleshCamera Variant", "DroneSkull Variant",
        "MinosPrime", "SisyphusPrime", "Cancerous Rodent", "Very Cancerous Rodent", "Mandalore", "Big Johninator", "Puppet"
    };

    /// <summary> List of internal names of all items. </summary>
    public static readonly string[] Items = new[]
    { ".Apple Bait", ".Maurice Bait", "SkullBlue", "SkullRed", "Soap", "Torch", "Florp Throwable" };

    /// <summary> List of internal names of all dev plushies. </summary>
    public static readonly string[] Plushies = new[]
    {
        "Jacob", "Mako", "HEALTH - Jake", "Dalia", "Jericho", "Meganeko", "Tucker", "BigRock", "Dawg", "Sam",
        "Cameron", "Gianni", "Salad", "Mandy", "Joy", "Weyte", "Heckteck", "Hakita", "Lenval", ". (CabalCrow) Variant",
        "Quetzal", "HEALTH - John", "PITR", "HEALTH - BJ", "Francis", "Vvizard", "Lucas", "Scott", "KGC", "."
    };

    /// <summary> List of readable names of all dev plushies needed for the /plushy command. </summary>
    public static readonly string[] PlushiesButReadable = new[]
    {
        "Jacob", "Maximilian", "Jake", "Dalia", "Jericho", "Meganeko", "Tucker", "BigRock", "Victoria", "Samuel",
        "Cameron", "Gianni", "Salad", "Mandy", "Joy", "Weyte", "Heckteck", "Hakita", "Lenval", "CabalCrow",
        "Quetzal", "John", "Pitr", "BJ", "Francis", "Vvizard", "Lucas", "Scott", "KGC", "V1"
    };

    /// <summary> Loads an enemy prefab by name. </summary>
    public static GameObject Enemy(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/Enemies/{name}.prefab");

    /// <summary> Loads an item prefab by name. </summary>
    public static GameObject Item(string name) =>
        AssetHelper.LoadPrefab($"Assets/Prefabs/{(name.StartsWith(".") ? $"Fishing/{name.Substring(1)}" : $"Items/{name}")}.prefab");

    /// <summary> Loads a dev plushy prefab by name. </summary>
    public static GameObject Plushy(string name) =>
        AssetHelper.LoadPrefab($"Assets/Prefabs/Items/DevPlushies/DevPlushie{(name.StartsWith(".") ? name.Substring(1) : $" ({name})")}.prefab");

    /// <summary> Loads the torch prefab. </summary>
    public static GameObject Torch() => AssetHelper.LoadPrefab("Assets/Prefabs/Levels/Interactive/Altar (Torch) Variant.prefab");

    /// <summary> Loads the blast explosion prefab. </summary>
    public static GameObject Blast() => AssetHelper.LoadPrefab("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Wave.prefab");

    /// <summary> Loads the shotgun pickup prefab. </summary>
    public static GameObject Shotgun() => AssetHelper.LoadPrefab("Assets/Prefabs/Weapons/Pickups/ShotgunPickUp.prefab");

    /// <summary> Loads a swordsmachine material by name. </summary>
    public static void SwordsMaterial(string name, Renderer output) =>
        Addressables.LoadAssetAsync<Material>($"Assets/Models/Enemies/SwordsMachine/{name}.mat").Task.ContinueWith(task => output.material = task.Result);
}
