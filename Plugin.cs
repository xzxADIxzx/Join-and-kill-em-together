namespace Jaket;

using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net;
using Jaket.UI;

[UKPlugin(GUID, NAME, VERSION, DESC, false, false)]
public class Plugin : UKMod
{
    const string GUID = "xzxADIxzx.Jaket";
    const string NAME = "Jaket";
    const string DESC = "Multikill is still in development, so I created my own multiplayer mod for ultrakill.\nAuthor: xzxADIxzx#7729 & Sowler#5518";
    const string VERSION = "0.0.1";

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
        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

        // net
        LobbyController.Load();

        // ui
        PlayerList.Build();
        Chat.Build();

        Initialized = true;
        Debug.Log("Jaket initialized.");
    }
}
