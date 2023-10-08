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

        // adds an event listener for plugin initialization
        SceneManager.sceneLoaded += (scene, mode) => Init();
    }

    /// <summary> Initializes the plugin if it has not already been initialized. </summary>
    public void Init()
    {
        // ui components can only be initialized in the main menu, because they need some resources
        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

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

        // initialize keybinds
        UKAPI.GetKeyBind("PLAYER LIST", KeyCode.F1).onPerformInScene.AddListener(PlayerList.Instance.Toggle);
        UKAPI.GetKeyBind("PLAYER INDICATOR", KeyCode.Z).onPerformInScene.AddListener(PlayerIndicators.Instance.Toggle);
        UKAPI.GetKeyBind("CHAT", KeyCode.Return).onPerformInScene.AddListener(Chat.Instance.Toggle);
        UKAPI.GetKeyBind("SCROOL MESSAGES UP", KeyCode.UpArrow).onPerformInScene.AddListener(() => Chat.Instance.ScrollMessages(true));
        UKAPI.GetKeyBind("SCROOL MESSAGES DOWN", KeyCode.DownArrow).onPerformInScene.AddListener(() => Chat.Instance.ScrollMessages(false));
        UKAPI.GetKeyBind("INITIATE SELF-DESTRUCTION", KeyCode.K).onPerformInScene.AddListener(Networking.LocalPlayer.SelfDestruct);

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Debug.Log("Jaket successfully initialized.");
    }
}
