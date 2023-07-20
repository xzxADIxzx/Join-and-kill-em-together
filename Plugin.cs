namespace Jaket;

using HarmonyLib;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.Net;
using Jaket.UI;

/// <summary> Plugin main class. Essentially only initializes all other components. </summary>
[UKPlugin(GUID, NAME, VERSION, DESC, false, false)]
public class Plugin : UKMod
{
    const string GUID = "xzxADIxzx.Jaket";
    const string NAME = "Jaket";
    const string DESC = "Multikill is still in development, so I created my own multiplayer mod for ultrakill.\nAuthor: xzxADIxzx#7729 & Sowler#5518";
    const string VERSION = "0.2.0";

    /// <summary> Plugin instance available everywhere. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public static bool Initialized;

    public override void OnModLoaded()
    {
        // save an instance for later use
        Instance = this;

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
        Enemies.Load();
        Weapons.Load();
        Bullets.Load(); // load it after weapons

        // initialize networking components
        Networking.Load();
        Entities.Load();

        // initialize ui components
        Utils.Load(); // gets some resources like images and fonts
        PlayerList.Build();
        PlayerIndicators.Build();
        Chat.Build();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // initialize keybinds
        UKAPI.GetKeyBind("PLAYER LIST", KeyCode.F1).onPerformInScene.AddListener(PlayerList.Instance.Toggle);
        UKAPI.GetKeyBind("PLAYER INDICATOR", KeyCode.Z).onPerformInScene.AddListener(PlayerIndicators.Instance.Toggle);
        UKAPI.GetKeyBind("CHAT", KeyCode.Return).onPerformInScene.AddListener(Chat.Toggle);

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Debug.Log("Jaket successfully initialized.");
    }
}
