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
    const string VERSION = "0.1.0";

    public static Plugin Instance;
    public static bool Initialized;

    public override void OnModLoaded()
    {
        Instance = this;

        UKAPI.GetKeyBind("PLAYER LIST", KeyCode.Tab).onPress.AddListener(PlayerList.Toggle);
        UKAPI.GetKeyBind("CHAT", KeyCode.Return).onPress.AddListener(Chat.Toggle);

        SceneManager.sceneLoaded += (scene, mode) => Init();
    }

    public void Init()
    {
        Utils.WasCheatsEnabled = false;
        Networking.Loading = false;

        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

        // content
        Enemies.Load();
        Weapons.Load();
        Bullets.Load(); // load it after weapons

        // net
        Networking.Load();

        // ui
        Utils.Load();
        PlayerList.Build();
        Chat.Build();

        // harmony
        new Harmony("Should I write something here?").PatchAll();

        Initialized = true;
        Debug.Log("Jaket initialized.");
    }
}
