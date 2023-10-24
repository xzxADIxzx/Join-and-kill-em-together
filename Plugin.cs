namespace Jaket;

using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI;
using Jaket.World;

/// <summary> Plugin main class. Essentially only initializes all other components. </summary>
[BepInPlugin("xzxADIxzx.Jaket", "Jaket", Version.CURRENT)]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Plugin instance available everywhere. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public static bool Initialized;
    /// <summary> Whether the Ultrapain mod is loaded. Needed to synchronize difficulty. </summary>
    public static bool UltrapainLoaded;

    /// <summary> Toggles Ultrapain difficulty. Placing it in a separate function is necessary to avoid errors. </summary>
    public static void TogglePain(bool unreal, bool real)
    { Ultrapain.Plugin.ultrapainDifficulty = unreal; Ultrapain.Plugin.realUltrapainDifficulty = real; }

    /// <summary> Writes Ultrapain difficulty data. </summary>
    public static void WritePain(Writer w)
    { w.Bool(Ultrapain.Plugin.ultrapainDifficulty); w.Bool(Ultrapain.Plugin.realUltrapainDifficulty); }

    public void Awake()
    {
        // save an instance for later use
        Instance = this;
        // rename the game object for a more presentable look
        gameObject.name = "Jaket";

        // adds an event listener to the scene loading
        Events.Load();
        // interface components and assets bundle can only be loaded from the main menu
        Events.OnMainMenuLoaded += Init;
    }

    /// <summary> Initializes the plugin if it has not already been initialized. </summary>
    public void Init()
    {
        if (Initialized) return;

        // notify players about the availability of an update so that they no longer whine to me about something not working
        Version.Check4Update();

        // check if Ultrapain is installed
        UltrapainLoaded = Chainloader.PluginInfos.ContainsKey("com.eternalUnion.ultraPain");

        // initialize content components
        DollAssets.Load();
        Enemies.Load();
        Weapons.Load();
        Bullets.Load(); // load it after weapons

        // initialize networking components
        Networking.Load(); // load before lobby controller
        Entities.Load();
        LobbyController.Load();

        // initialize world components
        World.World.Load(); // C# sucks
        Movement.Load();

        // initialize ui components
        WidescreenFix.Load();
        UI.UI.Load();
        UI.UI.Build();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Debug.Log("Jaket successfully initialized.");
    }
}
