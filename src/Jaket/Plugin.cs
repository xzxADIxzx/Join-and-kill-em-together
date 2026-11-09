namespace Jaket;

using BepInEx;
using UnityEngine;

using Jaket.Assets;
using Jaket.Harmony;
using Jaket.Input;
using Jaket.Net;
using Jaket.Net.Admin;
using Jaket.Sprays;
using Jaket.UI.Lib;
using Jaket.World;

[BepInPlugin("xzxADIxzx.Jaket", "Jaket", Version.CURRENT)]
public class PluginLoader : BaseUnityPlugin
{
    private void Awake() => Events.InternalSceneLoaded = () => Plugin.Instance ??= Create<Plugin>("Jaket");
}

/// <summary> Main class of the project that loads, initializes and stores different components of it. </summary>
public class Plugin : MonoBehaviour
{
    /// <summary> Singleton of the project. </summary>
    public static Plugin Instance;
    /// <summary> Whether the plugin has been initialized. </summary>
    public bool Initialized;

    private void Awake() => Keep(this);

    private void Start()
    {
        Log.Load();
        Log.Info("[INIT] Jaket bootloader has been created");

        Events.Load();
        Events.OnMainMenuLoad += Init;
        Application.wantsToQuit += Quit;
    }

    private bool Quit()
    {
        Log.Info("[INIT] Received an application shutdown notification");
        Log.Flush();

        Events.InternalFlushFinish = Application.Quit;
        Application.wantsToQuit -= Quit;
        return false;
    }

    private void Init()
    {
        if (Initialized) return;
        Initialized = true; // even if the plugin fails to load, it should not try again

        Log.Info("[INIT] Loading content...");

        Pal.Load();
        Tex.Load();
        ModAssets.Load();
        Bundle.Load();
        UI.UI.Build();
        Commands.Commands.Load();

        Log.Info("[INIT] Initializing network components...");

        Administration.Load();
        LobbyController.Load();
        Entities.Load();
        Networking.Load();

        Create<Emotes>("Emotes");
        Create<Movement>("Movement");

        SprayManager.Load();
        ActionList.Load();
        World.World.Load();
        Gameflow.Load();

        Log.Info("[INIT] Running postinit hooks...");

        Version.Check4Updates();
        Version.FetchCompatible();
        Patches.Load();
        Patches.LoadStatic();

        Log.Info("[INIT] Jaket has been initialized");
    }

    private void LateUpdate() => Events.Main();
}
