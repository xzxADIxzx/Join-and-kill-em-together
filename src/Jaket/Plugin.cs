namespace Jaket;

using BepInEx;
using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Sprays;
using Jaket.UI.Lib;
using Jaket.World;

/// <summary> Bootloader class needed to avoid destroying the mod by the game. </summary>
[BepInPlugin("xzxADIxzx.Jaket", "Jaket", Version.CURRENT)]
public class PluginLoader : BaseUnityPlugin
{
    private void Awake() => Events.InternalSceneLoaded = () => (Plugin.Instance ?? Create<Plugin>("Jaket")).Location = Info.Location;
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

    private void Awake() => DontDest(Instance = this); // save the instance of the mod for later use and prevent it from being destroyed by the game

    private void Start()
    {
        Log.Load();
        Log.Info("[INIT] Jaket bootloader has been created");

        Events.Load();
        Events.OnMainMenuLoad += Init;
    }

    private void OnApplicationQuit() => Log.Flush();

    private void Init()
    {
        if (Initialized) return;
        Initialized = true; // even if the plugin fails to load, it should not try again

        Log.Info("[INIT] Loading content...");

        // Pal.Load();
        Tex.Load();
        ModAssets.Load();

        Bundle.Load();
        UI.UI.Build();

        Log.Info("[INIT] Initializing network components...");
        // TODO obviously

        Log.Info("[INIT] Running postinit hooks...");

        Version.Check4Updates();
        Version.FetchCompatible();
        // TODO Harmony goes here

        Log.Info("[INIT] Jaket has been initialized");

        if (true) return;

        Pointers.Allocate();
        Stats.StartRecord();
        Tools.CacheAccId();

        Commands.Commands.Load();
        Bundle.Load();
        Weapons.Load();
        Bullets.Load();
        Enemies.Load();
        Items.Load();
        ModAssets.Load();

        Administration.Load();
        LobbyController.Load();
        Networking.Load();
        Entities.Load();

        World.World.Load();
        WorldActionsList.Load();
        Votes.Load();
        Movement.Load();
        SprayManager.Load();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();
    }
}
