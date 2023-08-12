namespace Jaket;

using BepInEx;
using HarmonyLib;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;

/// <summary> Plugin main class. Essentially only initializes all other components. </summary>
[BepInPlugin("xzxADIxzx.Jaket", "Jaket", "0.5.4")]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Plugin instance available everywhere. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public static bool Initialized;

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
        // get acquainted, this is a crutch
        Utils.WasCheatsEnabled = false;

        // ui components can only be initialized in the main menu, because they need some resources
        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

        // initialize content components
        DollAssets.Load();
        Enemies.Load();
        Weapons.Load();
        Bullets.Load(); // load it after weapons

        // initialize networking components
        Networking.Load();
        Entities.Load();
        World.Load();

        // initialize ui components
        WidescreenFix.Load();
        Utils.Load(); // gets some resources like images and fonts
        PlayerList.Build();
        PlayerIndicators.Build();
        Chat.Build();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // initialize keybinds
        UKAPI.GetKeyBind("PLAYER LIST", KeyCode.F1).onPerformInScene.AddListener(PlayerList.Instance.Toggle);
        UKAPI.GetKeyBind("PLAYER INDICATOR", KeyCode.Z).onPerformInScene.AddListener(PlayerIndicators.Instance.Toggle);
        UKAPI.GetKeyBind("CHAT", KeyCode.Return).onPerformInScene.AddListener(Chat.Instance.Toggle);
        UKAPI.GetKeyBind("INITIATE SELF-DESTRUCTION", KeyCode.K).onPerformInScene.AddListener(Networking.LocalPlayer.SelfDestruct);

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Debug.Log("Jaket successfully initialized.");
    }
}
