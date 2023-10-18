namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.World;

/// <summary> Global mod settings not related to the lobby. </summary>
public class Settings : CanvasSingleton<Settings>
{
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
    }

    private void Start()
    {
        UI.Shadow("Shadow", transform);
        UI.TableAT("Controls", transform, 0f, 352f, 696f, table =>
        {
            UI.Text("--CONTROLS--", table, 0f, 316f);
            UI.Button("RESET", table, 0f, 260f);

            int y = 0; // no way
            UI.KeyButton("lobby-tab", LobbyTab, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("player-list", PlayerList, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("settings", Settingz, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("player-indicators", PlayerIndicators, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("pointer", Pointer, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("chat", Chat, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("scroll-messages-up", ScrollUp, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("scroll-messages-down", ScrollDown, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("emoji-wheel", EmojiWheel, table, 0f, 196f + y-- * 56f);
            UI.KeyButton("self-destruction", SelfDestruction, table, 0f, 196f + y-- * 56f);
        });
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

    // <summary> Starts rebinding the given key. </summary>
    public void Rebind(string path, Text text, Image background)
    {
        this.path = path;
        this.text = text;
        this.background = background;

        background.color = new(1f, .7f, .1f);
        Rebinding = true;
    }
}
