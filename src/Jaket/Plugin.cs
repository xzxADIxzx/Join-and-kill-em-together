namespace Jaket;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Sprays;
using Jaket.World;

/// <summary> Bootloader class needed to avoid destroying the mod by the game. </summary>
[BepInPlugin("xzxADIxzx.Jaket", "Jaket", Version.CURRENT)]
public class PluginLoader : BaseUnityPlugin
{
    private void Awake() => SceneManager.sceneLoaded += (_, _) =>
    {
        if (Plugin.Instance == null) Tools.Create<Plugin>("Jaket").Location = Info.Location;
    };
}

/// <summary> Plugin main class. Essentially only initializes all other components. </summary>
public class Plugin : MonoBehaviour
{
    /// <summary> Plugin instance available everywhere. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public bool Initialized;
    /// <summary> Path to the dll file of the mod. </summary>
    public string Location;

    private void Awake() => DontDestroyOnLoad(Instance = this); // save the instance of the mod for later use and prevent it from being destroyed by the game

    private void Start()
    {
        // create output points for logs
        Log.Load();
        // note the fact that the mod is loading
        Log.Info("Loading jaket...");

        // adds an event listener to the scene loading
        Events.Load();
        // interface components and assets bundle can only be loaded from the main menu
        Events.OnMainMenuLoaded += Init;
    }

    private void OnApplicationQuit() => Log.Flush();

    private void Init()
    {
        if (Initialized) return;

        // notify players about the availability of an update so that they no longer whine to me about something not working
        Version.Check4Update();
        Pointers.Allocate();
        Stats.StartRecord();
        Tools.CacheAccId();

        Commands.Commands.Load();
        Bundle.Load();
        Enemies.Load();
        Weapons.Load();
        Bullets.Load();
        Items.Load();
        DollAssets.Load();

        Administration.Load();
        LobbyController.Load();
        Networking.Load();
        Entities.Load();

        World.World.Load();
        WorldActionsList.Load();
        Movement.Load();
        SprayManager.Load();

        UI.UIB.Load();
        UI.UI.Load();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Log.Info("Jaket initialized!");
    }
}
