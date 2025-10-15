namespace Jaket.Assets;

using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary> Loader that manages the assets of the game. </summary>
public static class GameAssets
{
    #region loading

    /// <summary> Asynchronously loads an asset by the given path. </summary>
    public static void LoadAsync<T>(string path, Cons<T> cons) => Addressables.LoadAssetAsync<T>(path).Task.ContinueWith(t => cons(t.Result));

    /// <summary> Loads a prefab by the given path. </summary>
    public static void Prefab(string path, Cons<GameObject> cons) => LoadAsync($"Assets/Prefabs/{path}", cons);

    /// <summary> Loads a particle by the given path. </summary>
    public static void Particle(string path, Cons<GameObject> cons) => LoadAsync($"Assets/Particles/{path}", cons);

    /// <summary> Loads a material by the given path. </summary>
    public static void Material(string path, Cons<Material> cons) => LoadAsync($"Assets/Models/{path}", cons);

    /// <summary> Loads a texture by the given path. </summary>
    public static void Texture(string path, Cons<Texture> cons) => LoadAsync($"Assets/Models/{path}", cons);

    /// <summary> Loads a shader by the given path. </summary>
    public static void Shader(string path, Cons<Shader> cons) => LoadAsync($"Assets/Shaders/{path}", cons);

    /// <summary> Loads a sound by the given path. </summary>
    public static void Sound(string path, Cons<AudioClip> cons) => LoadAsync($"Assets/Sounds/{path}", cons);

    #endregion
    #region content

    /// <summary> List of internal paths of all items. </summary>
    public static readonly string[] Items =
    {
        "Items/SkullBlue.prefab",
        "Items/SkullRed.prefab",
        "Items/Soap.prefab",
        "Items/Torch.prefab",
        "Items/Torch.prefab",
        "Items/Florp Throwable.prefab",
        "Fishing/Apple Bait.prefab",
        "Fishing/Maurice Bait.prefab",

        "Fishing/Fishes/Funny Fish!!!.prefab",
        "Fishing/Fishes/pitr fish.prefab",
        "Fishing/Fishes/Trout.prefab",
        "Fishing/Fishes/Amid Efil Fish.prefab",
        "Fishing/Fishes/Dusk Chomper.prefab",
        "Fishing/Fishes/Bomb Fish.prefab",
        "Fishing/Fishes/Gib Eyeball Fish.prefab",
        "Fishing/Fishes/IronLungFish.prefab",
        "Fishing/Fishes/Dope Fish.prefab",
        "Fishing/Fishes/Fish Stick.prefab",
        "Fishing/Fishes/Cooked Fish.prefab",
        "Fishing/Fishes/Shark Fish.prefab",
        "Fishing/Fishes/Burnt Stuff.prefab",

        "Items/DevPlushies/DevPlushie (Hakita).prefab",
        "Items/DevPlushies/DevPlushie (PITR).prefab",
        "Items/DevPlushies/DevPlushie (Dawg).prefab",
        "Items/DevPlushies/DevPlushie (Heckteck).prefab",
        "Items/DevPlushies/DevPlushie (CabalCrow) Variant.prefab",
        "Items/DevPlushies/DevPlushie (Lucas).prefab",
        "Items/DevPlushies/DevPlushie (Zombie).prefab",
        "Items/DevPlushies/DevPlushie (Francis).prefab",
        "Items/DevPlushies/DevPlushie (Jericho).prefab",
        "Items/DevPlushies/DevPlushie (BigRock).prefab",
        "Items/DevPlushies/DevPlushie (Mako).prefab",
        "Items/DevPlushies/DevPlushie (FlyingDog).prefab",
        "Items/DevPlushies/DevPlushie (Sam).prefab",
        "Items/DevPlushies/DevPlushie (Salad).prefab",
        "Items/DevPlushies/DevPlushie (Meganeko).prefab",
        "Items/DevPlushies/DevPlushie (KGC).prefab",
        "Items/DevPlushies/DevPlushie (HEALTH - BJ).prefab",
        "Items/DevPlushies/DevPlushie (HEALTH - Jake).prefab",
        "Items/DevPlushies/DevPlushie (HEALTH - John).prefab",
        "Items/DevPlushies/DevPlushie (King Gizzard).prefab",
        "Items/DevPlushies/DevPlushie (Quetzal).prefab",
        "Items/DevPlushies/DevPlushie (Gianni).prefab",
        "Items/DevPlushies/DevPlushie (Weyte).prefab",
        "Items/DevPlushies/DevPlushie (Lenval).prefab",
        "Items/DevPlushies/DevPlushie (Joy).prefab",
        "Items/DevPlushies/DevPlushie (Mandy).prefab",
        "Items/DevPlushies/DevPlushie (Cameron).prefab",
        "Items/DevPlushies/DevPlushie (Dalia).prefab",
        "Items/DevPlushies/DevPlushie (Tucker).prefab",
        "Items/DevPlushies/DevPlushie (Scott).prefab",
        "Items/DevPlushies/DevPlushie (Jacob).prefab",
        "Items/DevPlushies/DevPlushie (Vvizard).prefab",
        "Items/DevPlushies/DevPlushie.prefab",
    };

    /// <summary> List of readable names of all plushies. </summary>
    public static readonly string[] Plushies =
    {
        "hakita", "pitr", "victoria", "heckteck", "cabalcrow", "lucas", "zombie", "francis", "jericho", "bigrock", "mako", "flyingdog", "samuel", "salad", "meganeko", "kgc", "benjamin", "jake", "john", "lizard", "quetzal", "gianni", "weyte", "lenval", "joy", "mandy", "cameron", "dalia", "tucker", "scott", "jacob", "vvizard", "v1", "v2", "v3", "xzxadixzx", "owlnotsowler"
    };

    /// <summary> List of internal paths of all weapons. </summary>
    public static readonly string[] Weapons =
    {
        "Weapons/Revolver Pierce.prefab",   "Weapons/Alternative Revolver Pierce.prefab",
        "Weapons/Revolver Ricochet.prefab", "Weapons/Alternative Revolver Ricochet.prefab",
        "Weapons/Revolver Twirl.prefab",    "Weapons/Alternative Revolver Twirl.prefab",

        "Weapons/Shotgun Grenade.prefab",   "Weapons/Hammer Grenade.prefab",
        "Weapons/Shotgun Pump.prefab",      "Weapons/Hammer Pump.prefab",
        "Weapons/Shotgun Saw.prefab",       "Weapons/Hammer Saw.prefab",

        "Weapons/Nailgun Magnet.prefab",    "Weapons/Sawblade Launcher Magnet.prefab",
        "Weapons/Nailgun Overheat.prefab",  "Weapons/Sawblade Launcher Overheat.prefab",
        "Weapons/Nailgun Zapper.prefab",    "Weapons/Sawblade Launcher Zapper.prefab",

        "Weapons/Railcannon Electric.prefab",
        "Weapons/Railcannon Harpoon.prefab",
        "Weapons/Railcannon Malicious.prefab",

        "Weapons/Rocket Launcher Freeze.prefab",
        "Weapons/Rocket Launcher Cannonball.prefab",
        "Weapons/Rocket Launcher Napalm.prefab",
    };

    /// <summary> List of internal paths of all projectiles. </summary>
    public static readonly string[] Projectiles =
    {
        "Attacks and Projectiles/Grenade.prefab",
        "Attacks and Projectiles/NailAlt.prefab",
        "Attacks and Projectiles/NailAltFodder.prefab",
        "Attacks and Projectiles/NailAltHeated.prefab",
        "Attacks and Projectiles/Rocket.prefab",
        "Attacks and Projectiles/Cannonball.prefab",
    };

    /// <summary> List of internal paths of all explosions. </summary>
    public static readonly string[] Explosions =
    {
        "Attacks and Projectiles/PhysicalShockwavePlayer.prefab",
        "Attacks and Projectiles/Explosions/Explosion Wave.prefab",
        "Attacks and Projectiles/Explosions/Explosion.prefab",
        "Attacks and Projectiles/Explosions/Explosion Hammer Weak.prefab",
        "Attacks and Projectiles/Explosions/Explosion Super.prefab",
    };

    /// <summary> List of internal paths of all hammer particles.  </summary>
    public static readonly string[] Particles =
    {
        "HammerHitLight.prefab",
        "HammerHitMedium.prefab",
        "HammerHitHeavy.prefab",
    };

    #endregion
    #region obsolete content

    /// <summary> List of items that mustn't be synchronized, because they are not items at all. </summary>
    public static readonly string[] ItemExceptions =
    { "Minotaur", "Tram (3)", "BombTrigger", "BombStationTramTeleporterKey", "Checker" };

    #endregion
    #region obsolete loading

    private static GameObject Prefab(string name) => AssetHelper.LoadPrefab($"Assets/Prefabs/{name}.prefab");

    /// <summary> Loads the torch prefab. </summary>
    public static GameObject Torch() => Prefab("Levels/Interactive/Altar (Torch) Variant");

    /// <summary> Loads the shotgun pickup prefab. </summary>
    public static GameObject Shotgun() => Prefab("Weapons/Pickups/ShotgunPickUp");

    /// <summary> Loads a swordsmachine material by name. </summary>
    public static void SwordsMaterial(string name, Renderer output) => Material($"Enemies/SwordsMachine/{name}.mat", mat => output.material = mat);

    /// <summary> Loads an insurrectionist material by name. </summary>
    public static void SisyMaterial(string name, Renderer[] output) => Material($"Enemies/Sisyphus/{name}.mat", mat => output[0].material = output[1].material = mat);

    #endregion
}
