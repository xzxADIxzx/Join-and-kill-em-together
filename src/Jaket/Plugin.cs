namespace Jaket;

using BepInEx;
using HarmonyLib;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Sprays;
using Jaket.World;

/// <summary> Plugin main class. Essentially only initializes all other components. </summary>
[BepInPlugin("xzxADIxzx.Jaket", "Jaket", Version.CURRENT)]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Plugin instance available everywhere. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public static bool Initialized;

    private void Awake()
    {
        // save an instance for later use
        Instance = this;
        // rename the game object for a more presentable look
        name = "Jaket";

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

    /// <summary> Initializes the plugin if it has not already been initialized. </summary>
    public void Init()
    {
        if (Initialized) return;

        // notify players about the availability of an update so that they no longer whine to me about something not working
        Version.Check4Update();

        Commands.Commands.Load();
        Bundle.Load();
        DollAssets.Load();
        Enemies.Load();
        Weapons.Load();
        Bullets.Load();
        Items.Load();

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
