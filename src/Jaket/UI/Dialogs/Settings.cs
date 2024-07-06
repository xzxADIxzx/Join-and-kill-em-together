namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.World;

using static Pal;
using static Rect;

/// <summary> Global mod settings not related to the lobby. </summary>
public class Settings : CanvasSingleton<Settings>
{
    static PrefsManager pm => PrefsManager.Instance;

    #region general

    /// <summary> Id of the currently selected language. </summary>
    public static int Language;
    /// <summary> 0 - default (depending on whether the player is in the lobby or not), 1 - always green, 2 - always blue/red. </summary>
    public static int FeedColor, KnuckleColor;
    /// <summary> Whether freeze frames are disabled. </summary>
    public static bool DisableFreezeFrames;

    #endregion
    #region controls

    /// <summary> List of internal names of all key bindings. </summary>
    public static readonly string[] Keybinds =
    { "chat", "scroll-messages-up", "scroll-messages-down", "lobby-tab", "player-list", "settings", "player-indicators", "player-information", "emoji-wheel", "pointer", "spray", "self-destruction" };

    /// <summary> Array with current control settings. </summary>
    public static KeyCode[] CurrentKeys => new[]
    { Chat, ScrollUp, ScrollDown, LobbyTab, PlayerList, Settingz, PlayerIndicators, PlayerInfo, EmojiWheel, Pointer, Spray, SelfDestruction };

    /// <summary> List of all key bindings in the mod. </summary>
    public static KeyCode Chat, ScrollUp, ScrollDown, LobbyTab, PlayerList, Settingz, PlayerIndicators, PlayerInfo, EmojiWheel, Pointer, Spray, SelfDestruction;

    /// <summary> Gets the key binding value from its path. </summary>
    public static KeyCode GetKey(string path, KeyCode def) => (KeyCode)pm.GetInt($"jaket.binds.{path}", (int)def);

    /// <summary> Returns the name of the given key. </summary>
    public static string KeyName(KeyCode key) => key switch
    {
        KeyCode.LeftAlt => "LEFT ALT",
        KeyCode.RightAlt => "RIGHT ALT",
        KeyCode.LeftShift => "LEFT SHIFT",
        KeyCode.RightShift => "RIGHT SHIFT",
        KeyCode.LeftControl => "LEFT CONTROL",
        KeyCode.RightControl => "RIGHT CONTROL",
        KeyCode.Return => "ENTER",
        KeyCode.CapsLock => "CAPS LOCK",
        _ => key.ToString().Replace("Alpha", "").Replace("Keypad", "Num ").ToUpper()
    };

    #endregion
    #region tts

    // <summary> Sam's voice volume. Limited by interval from 0 to 100. </summary>
    public static int TTSVolume
    {
        get => pm.GetInt("jaket.tts.volume", 60);
        set
        {
            DollAssets.Mixer?.SetFloat("Volume", value / 2f - 30f); // the value should be between -30 and 20 decibels
            pm.SetInt("jaket.tts.volume", value);
        }
    }

    /// <summary> Whether auto TTS is enabled. </summary>
    public static bool AutoTTS
    {
        get => pm.GetBool("jaket.tts.auto");
        set => pm.SetBool("jaket.tts.auto", value);
    }

    #endregion

    /// <summary> Whether a binding is being reassigned right now. </summary>
    public bool Rebinding;
    /// <summary> Components of a key button and the path to the keybind. </summary>
    private string path; Text text; Image background;
    /// <summary> General settings buttons. </summary>
    private Button lang, feed, knkl;

    /// <summary> Loads and applies all settings. </summary>
    public static void Load()
    {
        Language = Bundle.LoadedLocale;
        FeedColor = pm.GetInt("jaket.feed-color");
        KnuckleColor = pm.GetInt("jaket.knkl-color");
        DisableFreezeFrames = pm.GetBool("jaket.disable-freeze", true);

        Chat = GetKey("chat", KeyCode.Return);
        ScrollUp = GetKey("scroll-messages-up", KeyCode.UpArrow);
        ScrollDown = GetKey("scroll-messages-down", KeyCode.DownArrow);
        LobbyTab = GetKey("lobby-tab", KeyCode.F1);
        PlayerList = GetKey("player-list", KeyCode.F2);
        Settingz = GetKey("settings", KeyCode.F3);
        PlayerIndicators = GetKey("player-indicators", KeyCode.Z);
        PlayerInfo = GetKey("player-information", KeyCode.X);
        EmojiWheel = GetKey("emoji-wheel", KeyCode.B);
        Pointer = GetKey("pointer", KeyCode.Mouse2);
        Spray = GetKey("spray", KeyCode.T);
        SelfDestruction = GetKey("self-destruction", KeyCode.K);

        DollAssets.Mixer?.SetFloat("Volume", TTSVolume / 2f - 30f);
    }

    private void Start()
    {
        UIB.Shadow(transform);
        UIB.Table("General", "#settings.general", transform, Tlw(16f + 328f / 2f, 328f), table =>
        {
            UIB.Button("#settings.reset", table, Btn(0f, 68f), clicked: ResetGeneral);

            lang = UIB.Button("", table, Btn(0f, 116f), clicked: () =>
            {
                pm.SetString("jaket.locale", Bundle.Codes[Language = ++Language % Bundle.Codes.Length]);
                Rebuild();
            });

            UIB.Text("FEEDBACKER:", table, Btn(0f, 164f), align: TextAnchor.MiddleLeft);
            feed = UIB.Button("", table, Btn(80f, 164f) with { Width = 160f }, clicked: () =>
            {
                pm.SetInt("jaket.feed-color", FeedColor = ++FeedColor % 3);
                Rebuild();
            });

            UIB.Text("KNUCKLE:", table, Btn(0f, 212f), align: TextAnchor.MiddleLeft);
            knkl = UIB.Button("", table, Btn(80f, 212f) with { Width = 160f }, clicked: () =>
            {
                pm.SetInt("jaket.knkl-color", KnuckleColor = ++KnuckleColor % 3);
                Rebuild();
            });

            UIB.Toggle("#settings.freeze", table, Tgl(0f, 256f), 20, _ =>
            {
                pm.SetBool("jaket.disable-freeze", DisableFreezeFrames = _);
            }).isOn = DisableFreezeFrames;

            UIB.Button("#settings.sprays", table, Btn(0f, 300f), clicked: SpraySettings.Instance.Toggle);
        });
        UIB.Table("Controls", "#settings.controls", transform, Tlw(360f + 576f / 2f, 576f), table =>
        {
            UIB.Button("#settings.reset", table, Btn(0f, 68f), clicked: ResetControls);

            for (int i = 0; i < Keybinds.Length; i++)
                UIB.KeyButton(Keybinds[i], CurrentKeys[i], table, Tgl(0f, 112f + i * 40f));
        });

        Version.Label(transform);
        Rebuild();
    }

    private void OnGUI()
    {
        if (!Rebinding) return;

        var current = Event.current; // receive the event and check whether any key is pressed
        if (!current.isKey && !current.isMouse && !current.shift) return;

        background.color = new(0f, 0f, 0f, .5f);
        Rebinding = false;

        // cancel key binding remapping
        if (current.keyCode == KeyCode.Escape || (current.isMouse && current.button == 0)) return;

        KeyCode key = current.isKey
            ? current.keyCode
            : current.isMouse
                ? KeyCode.Mouse0 + current.button
                : Input.GetKeyDown(KeyCode.LeftShift)
                    ? KeyCode.LeftShift
                    : KeyCode.RightShift;

        text.text = KeyName(key);
        pm.SetInt($"jaket.binds.{path}", (int)key);
        Load();
    }

    // <summary> Toggles visibility of the settings. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideLeftGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();
    }

    /// <summary> Rebuilds the settings to update some labels. </summary>
    public void Rebuild()
    {
        string Mode(int mode) => Bundle.Get(mode switch
        {
            0 => "settings.default",
            1 => "settings.green",
            2 => "settings.vanilla",
            _ => "lobby-tab.default"
        });

        lang.GetComponentInChildren<Text>().text = Bundle.Locales[Language];
        feed.GetComponentInChildren<Text>().text = Mode(FeedColor);
        knkl.GetComponentInChildren<Text>().text = Mode(KnuckleColor);

        // update the color of the feedbacker and knuckleblaster
        Events.OnWeaponChanged.Fire();
    }

    // <summary> Starts rebinding the given key. </summary>
    public void Rebind(string path, Text text, Image background)
    {
        this.path = path;
        this.text = text;
        this.background = background;

        background.color = orange;
        Rebinding = true;
    }

    #region reset

    private void ResetGeneral()
    {
        pm.SetString("jaket.locale", Bundle.Codes[Bundle.LoadedLocale]);
        pm.DeleteKey("jaket.feed-color");
        pm.DeleteKey("jaket.knkl-color");
        pm.DeleteKey("jaket.disable-freeze");

        Load();
        Rebuild();
        transform.GetChild(1).GetChild(7).GetComponent<Toggle>().isOn = DisableFreezeFrames;
    }

    private void ResetControls()
    {
        foreach (var name in Keybinds) pm.DeleteKey($"jaket.binds.{name}");

        Load();
        for (int i = 0; i < Keybinds.Length; i++)
            transform.GetChild(2).GetChild(i + 2).GetChild(0).GetChild(0).GetComponent<Text>().text = KeyName(CurrentKeys[i]);
    }

    #endregion
}
