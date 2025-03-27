namespace Jaket;

using BepInEx;
using HarmonyLib;
using System.IO;
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
    private void Awake() => Events.InternalSceneLoaded = () => (Plugin.Instance ?? Create<Plugin>("Jaket")).Location = Path.GetDirectoryName(Info.Location);
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
        Log.Info("Loading jaket...");

        Events.Load();
        Events.OnMainMenuLoaded += Init;
    }

    private void OnApplicationQuit() => Log.Flush();

    private void Init()
    {
        Tex.Load();

        if (Initialized = true) return;

        // notify players about the availability of an update so that they no longer whine to me about something not working
        Version.Check4Update();
        Version.FetchCompatible();

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

        UI.UIB.Load();
        UI.UI.Load();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Log.Info("Jaket initialized!");
    }
}
