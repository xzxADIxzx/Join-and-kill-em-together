namespace Jaket.Assets;

using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary> Class that works with the assets of the game. </summary>
public class GameAssets
{
    #region content

    /// <summary> List of items that mustn't be synchronized, because they are not items at all. </summary>
    public static readonly string[] ItemExceptions =
    { "Minotaur", "Tram (3)", "BombTrigger", "BombStationTramTeleporterKey", "Checker" };

    /// <summary> List of internal names of all enemies. </summary>
    public static readonly string[] Enemies =
    {
        "Zombie", "Projectile Zombie", "Super Projectile Zombie", "ShotgunHusk", "MinosBoss", "Stalker", "Sisyphus", "Ferryman",
        "SwordsMachineNonboss", "Drone", "Streetcleaner", "Mindflayer", "V2", "V2 Green Arm Variant", "Turret", "Gutterman",
        "Guttertank", "Spider", "StatueEnemy", "Mass", "Idol", "Mannequin", "Minotaur", "Virtue",
        "Gabriel", "Gabriel 2nd Variant", "Wicked", "Flesh Prison", "DroneFlesh", "Flesh Prison 2", "DroneFleshCamera Variant", "DroneSkull Variant",
        "MinosPrime", "SisyphusPrime", "Cancerous Rodent", "Very Cancerous Rodent", "Mandalore", "Big Johninator", "Puppet"
    };

    /// <summary> List of internal names of all items. </summary>
    public static readonly string[] Items =
    { ".Apple Bait", ".Maurice Bait", "SkullBlue", "SkullRed", "Soap", "Torch", "Florp Throwable" };

    /// <summary> List of internal names of all dev plushies. </summary>
    public static readonly string[] Plushies =
    {
        "Hakita", "PITR", "Dawg", "Heckteck", ". (CabalCrow) Variant", "Lucas", "Francis", "Jericho", "BigRock", "Mako",
        "Sam", "Salad", "Meganeko", "KGC", "HEALTH - BJ", "HEALTH - Jake", "HEALTH - John", "Quetzal", "Gianni", "Weyte",
        "Lenval", "Joy", "Mandy", "Cameron", "Dalia", "Tucker", "Scott", "Jacob", "Vvizard", ".", ".", ".", ".", "."
    };

    /// <summary> List of readable names of all dev plushies. </summary>
    public static readonly string[] PlushiesButReadable =
    {
        "hakita", "pitr", "victoria", "heckteck", "cabalcrow", "lucas", "francis", "jericho", "bigrock", "mako",
        "samuel", "salad", "meganeko", "kgc", "bj", "jake", "john", "quetzal", "gianni", "weyte",
        "lenval", "joy", "mandy", "cameron", "dalia", "tucker", "scott", "jacob", "vvizard", "v1", "v2", "v3", "xzxadixzx", "sowler"
    };

    #endregion
    #region tools

    private static GameObject Prefab(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/{name}");

    private static void Material(string name, Action<Material> cons) => Addressables.LoadAssetAsync<Material>($"Assets/Models/{name}").Task.ContinueWith(task => cons(task.Result));

    #endregion
    #region loading

    public static GameObject Enemy(string name) => Prefab($"Enemies/{name}.prefab");

    public static GameObject Item(string name) => Prefab(name.StartsWith(".") ? $"Fishing/{name.Substring(1)}.prefab" : $"Items/{name}.prefab");

    public static GameObject Plushie(string name) => Prefab($"Items/DevPlushies/DevPlushie{(name.StartsWith(".") ? name.Substring(1) : $" ({name})")}.prefab");

    /// <summary> Loads the torch prefab. </summary>
    public static GameObject Torch() => Prefab("Levels/Interactive/Altar (Torch) Variant.prefab");

    /// <summary> Loads the blast explosion prefab. </summary>
    public static GameObject Blast() => Prefab("Attacks and Projectiles/Explosions/Explosion Wave.prefab");

    /// <summary> Loads the shotgun pickup prefab. </summary>
    public static GameObject Shotgun() => Prefab("Weapons/Pickups/ShotgunPickUp.prefab");

    /// <summary> Loads a swordsmachine material by name. </summary>
    public static void SwordsMaterial(string name, Renderer output) => Material($"Enemies/SwordsMachine/{name}.mat", mat => output.material = mat);

    /// <summary> Loads an insurrectionist material by name. </summary>
    public static void SisyMaterial(string name, Renderer[] output) => Material($"Enemies/Sisyphus/{name}.mat", mat => output[0].material = output[1].material = mat);

    #endregion
}
