namespace Jaket;

using HarmonyLib;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.Net;
using Jaket.UI;

[UKPlugin(GUID, NAME, VERSION, DESC, false, false)]
public class Plugin : UKMod
{
    const string GUID = "xzxADIxzx.Jaket";
    const string NAME = "Jaket";
    const string DESC = "Multikill is still in development, so I created my own multiplayer mod for ultrakill.\nAuthor: xzxADIxzx#7729 & Sowler#5518";
    const string VERSION = "0.2.0";

    public static Plugin Instance;
    public static bool Initialized;

    public override void OnModLoaded()
    {
        Instance = this;
        SceneManager.sceneLoaded += (scene, mode) => Init();
    }

    public void Init()
    {
        Utils.WasCheatsEnabled = false;
        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

        // content
        Enemies.Load();
        Weapons.Load();
        Bullets.Load(); // load it after weapons

        // net
        Networking.Load();
        Entities.Load();

        // ui
        Utils.Load();
        PlayerList.Build();
        PlayerIndicators.Build();
        Chat.Build();

        // harmony
        new Harmony("Should I write something here?").PatchAll();

        // keybinds
        UKAPI.GetKeyBind("PLAYER LIST", KeyCode.F1).onPerformInScene.AddListener(PlayerList.Instance.Toggle);
        UKAPI.GetKeyBind("PLAYER INDICATOR", KeyCode.Z).onPerformInScene.AddListener(PlayerIndicators.Instance.Toggle);
        UKAPI.GetKeyBind("CHAT", KeyCode.Return).onPerformInScene.AddListener(Chat.Toggle);

        Initialized = true;
        Debug.Log("Jaket initialized.");
    }
}
