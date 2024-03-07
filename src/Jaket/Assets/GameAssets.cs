namespace Jaket.Assets;

using UnityEngine;

/// <summary> Class that works with the assets of the game. </summary>
public class GameAssets
{
    /// <summary> List of rooms that mustn't be unloaded in multiplayer, because don't have doors that would load them. </summary>
    public static readonly string[] RoomExceptions = new[]
    {
        /* 0-1 */ "3 - Gun Room", "4 - Hallway",
        /* 0-2 */ "1A - First Room", "5B - Secret Arena", "9 - Crushers Arena", "9B - Swordsmachine Arena Hallway",
        /* 0-3 */ "1 - Main Room - Floor 1", "2 - Side Hallway - Floor 1", "4 - Side Stairway - Floor 1-2", "10 - Main Room - Floor 2",
        /* 0-4 */ "1A - Hallway 1A",
        /* 0-5 */ "1 - Opening Hallway",
        /* 1-1 */ "2 - Indoors", "3 - Skull Field",
        /* 1-2 */ "1 - First Room", "7B - Lava Room",
        /* 1-3 */ "1 - Water Hall",
        /* 1-4 */ "1 - Opener", "3DL - Super Door", "3TL - Window Room",
        /* 2-2 */ "2 - Tunnel 1 ",
        /* 2-3 */ "1 - Main Hall", "1-3 Connectors", "3-2 Connector", "4 - End Hallway", "5 - Final Arena",
        /* 3-1/2 */ "1 - Opening",
        /* 4-2 */ "6A - Indoor Garden", "6B - Outdoor Arena",
        /* 4-3 */ "2 - Torches Arena", "4 - Pit Room",
        /* 5-1 */ "2 - Elevator",
        /* 5-3 */ "1 - Hallway", "2B - Forward Pathway",
        /* 6-1 */ "1 - Entryway",
    };

    /// <summary> List of items that mustn't be synchronized, because they are not items at all. </summary>
    public static readonly string[] ItemExceptions = new[]
    { "Minotaur", "Tram (3)", "BombTrigger", "BombStationTramTeleporterKey", "Checker" };

    /// <summary> List of internal names of all enemies. </summary>
    public static readonly string[] Enemies = new[]
    {
        "Zombie", "Projectile Zombie", "Super Projectile Zombie", "ShotgunHusk", "MinosBoss", "Stalker", "Sisyphus", "Ferryman",
        "SwordsMachineNonboss", "Drone", "Streetcleaner", "Mindflayer", "V2", "V2 Green Arm Variant", "Turret", "Gutterman",
        "Guttertank", "Brain", "CentaurRocketLauncher", "CentaurMortar", "CentaurTower", "Spider", "StatueEnemy", "Mass",
        "Idol", "Mannequin", "Minotaur", "Virtue", "Gabriel", "Gabriel 2nd Variant", "Wicked", "Flesh Prison",
        "DroneFlesh", "Flesh Prison 2", "DroneFleshCamera Variant", "DroneSkull Variant", "MinosPrime", "SisyphusPrime", "Cancerous Rodent", "Very Cancerous Rodent",
        "Mandalore", "Big Johninator", "Puppet"
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
}
