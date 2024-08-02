namespace Jaket;

using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.Rendering;

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

    /// <summary> List of mods compatible with Jaket. </summary>
    public static readonly string[] Compatible = { "Jaket", "CrosshairColorFixer", "IntroSkip", "Healthbars", "RcHud", "PluginConfigurator", "AngryLevelLoader" };
    /// <summary> Whether at least on incompatible mod is loaded. </summary>
    public bool HasIncompatibility;

    private int prevhealth = MonoSingleton<NewMovement>.Instance.hp;

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
        Votes.Load();
        Movement.Load();
        SprayManager.Load();

        UI.UIB.Load();
        UI.UI.Load();

        // initialize harmony and patch all the necessary classes
        new Harmony("Should I write something here?").PatchAll();

        // check if there is any incompatible mods
        HasIncompatibility = Chainloader.PluginInfos.Values.Any(info => !Compatible.Contains(info.Metadata.Name));

        // mark the plugin as initialized and log a message about it
        Initialized = true;
        Log.Info("Jaket initialized!");
    }
    private void Update() {
        if (MonoSingleton<NewMovement>.Instance.hp != prevhealth) {
MonoSingleton<ColorBlindSettings>.Instance.filthColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.strayColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.schismColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.shotgunnerColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.stalkerColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.sisyphusColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.ferrymanColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.droneColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.streetcleanerColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.swordsmachineColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.mindflayerColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.v2Color = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.turretColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.guttermanColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.guttertankColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.maliciousColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.cerberusColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.idolColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.mannequinColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
MonoSingleton<ColorBlindSettings>.Instance.virtueColor = new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F);
            MonoSingleton<ColorBlindSettings>.Instance.SetEnemyColor((EnemyType)1, new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F));
            //FieldInfo field = MonoSingleton<PostProcessV2_Handler>.Instance.GetType().GetField("outlineProcessor", BindingFlags.NonPublic | BindingFlags.Instance);
            //Material outlineProcessor = (Material) field.GetValue(MonoSingleton<PostProcessV2_Handler>.Instance);
            //outlineProcessor.SetColor("_OutlineColor", new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F));
            //outlineProcessor.SetColor("_Color", new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F));
            //MonoSingleton<PostProcessV2_Handler>.Instance.postProcessV2_VSRM.SetColor("_Color", new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F));
            //MonoSingleton<PostProcessV2_Handler>.Instance.postProcessV2_VSRM.SetColor("_OutlineColor", new Color(((float)100 - (float)MonoSingleton<NewMovement>.Instance.hp)/100, (float)MonoSingleton<NewMovement>.Instance.hp/100, 0F, 1F));
            prevhealth = MonoSingleton<NewMovement>.Instance.hp;
        }
    }
}
