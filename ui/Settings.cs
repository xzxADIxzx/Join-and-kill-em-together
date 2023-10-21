namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.World;

/// <summary> Global mod settings not related to the lobby. </summary>
public class Settings : CanvasSingleton<Settings>
{
    /// <summary> List of internal names of all key bindings. </summary>
    public static readonly string[] Keybinds =
    { "lobby-tab", "player-list", "settings", "player-indicators", "pointer", "chat", "scroll-messages-up", "scroll-messages-down", "emoji-wheel", "self-destruction" };

    /// <summary> List of all key bindings in the mod. </summary>
    public static KeyCode LobbyTab, PlayerList, Settingz, PlayerIndicators, Pointer, Chat, ScrollUp, ScrollDown, EmojiWheel, SelfDestruction;
    /// <summary> Gets the key binding value from its path. </summary>
    public static KeyCode GetKey(string path, KeyCode def) => (KeyCode)PrefsManager.Instance.GetInt($"jaket.binds.{path}", (int)def);

    /// <summary> Whether a key binding is being reassigned. </summary>
    public bool Rebinding;
    /// <summary> Components of a key remap button and the path to the keybind. </summary>
    private string path; Text text; Image background;

    /// <summary> Loads and applies all settings. </summary>
    public static void Load()
    {
        LobbyTab = GetKey("lobby-tab", KeyCode.F1);
        PlayerList = GetKey("player-list", KeyCode.F2);
        Settingz = GetKey("settings", KeyCode.F3);
        PlayerIndicators = GetKey("player-indicators", KeyCode.Z);
        Pointer = GetKey("pointer", KeyCode.Mouse2);
        Chat = GetKey("chat", KeyCode.Return);
        ScrollUp = GetKey("scroll-messages-up", KeyCode.UpArrow);
        ScrollDown = GetKey("scroll-messages-down", KeyCode.DownArrow);
        EmojiWheel = GetKey("emoji-wheel", KeyCode.B);
        SelfDestruction = GetKey("self-destruction", KeyCode.K);

        DollAssets.Mixer?.SetFloat("Volume", PrefsManager.Instance.GetInt("jaket.tts.volume", 60) / 2f - 30f);
    }

    private void Start()
    {
        UI.Shadow("Shadow", transform);
        UI.TableAT("Controls", transform, 0f, 352f, 696f, table =>
        {
            UI.Text("--CONTROLS--", table, 0f, 316f);
            UI.Button("RESET", table, 0f, 260f, clicked: ResetKeybinds);

            var list = new[] { LobbyTab, PlayerList, Settingz, PlayerIndicators, Pointer, Chat, ScrollUp, ScrollDown, EmojiWheel, SelfDestruction };
            for (int i = 0; i < list.Length; i++)
                UI.KeyButton(Keybinds[i], list[i], table, 0f, 196f - i * 56f);
        });

        Version.Label(transform);
        WidescreenFix.MoveDown(transform);
    }

    private void OnGUI()
    {
        if (!Rebinding) return;

        var current = Event.current; // receive the event and check whether any key is pressed
        if (!current.isKey && !current.isMouse && !current.shift) return;

        background.color = new(0f, 0f, 0f, .5f);
        Rebinding = false;

        // cancel key binding remapping
        if (current.keyCode == KeyCode.Escape) return;

        KeyCode key = current.isKey
            ? current.keyCode
            : current.isMouse
                ? KeyCode.Mouse0 + current.button
                : Input.GetKeyDown(KeyCode.LeftShift)
                    ? KeyCode.LeftShift
                    : KeyCode.RightShift;

        text.text = ControlsOptions.GetKeyName(key);
        PrefsManager.Instance.SetInt($"jaket.binds.{path}", (int)key);
        Load(); // update control settings
    }

    // <summary> Toggles visibility of settings. </summary>
    public void Toggle()
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown) return;

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();
    }

    #region controls

    // <summary> Resets control settings. </summary>
    public void ResetKeybinds()
    {
        // remove keys from preferences to reset key bindings to defaults
        foreach (var name in Keybinds) PrefsManager.Instance.DeleteKey($"jaket.binds.{name}");
        Load(); // load default values

        // update the labels in the buttons
        var list = new[] { LobbyTab, PlayerList, Settingz, PlayerIndicators, Pointer, Chat, ScrollUp, ScrollDown, EmojiWheel, SelfDestruction };
        for (int i = 0; i < list.Length; i++)
            transform.GetChild(1).GetChild(i + 2).GetChild(0).GetComponentInChildren<Text>().text = ControlsOptions.GetKeyName(list[i]);
    }

    // <summary> Starts rebinding the given key. </summary>
    public void Rebind(string path, Text text, Image background)
    {
        this.path = path;
        this.text = text;
        this.background = background;

        background.color = new(1f, .7f, .1f);
        Rebinding = true;
    }

    #endregion
    #region other

    // <summary> Changes and saves Sam's voice volume. </summary>
    public void ChangeTTSVolume(int volume)
    {
        DollAssets.Mixer?.SetFloat("Volume", volume / 2f - 30f); // the value should be between -30 and 20 decibels
        PrefsManager.Instance.SetInt("jaket.tts.volume", volume);
    }

    #endregion
}
