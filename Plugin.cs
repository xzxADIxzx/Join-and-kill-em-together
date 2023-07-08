namespace Jaket;

using UMM;
using UnityEngine.SceneManagement;

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

        SceneManager.sceneLoaded += (scene, mode) => Init();
    }

    public void Init()
    {
        if (Initialized || SceneHelper.CurrentScene != "Main Menu") return;

        // ui
        PlayerList.Build();

        Initialized = true;
        Debug.Log("Jaket initialized.");
    }
}
