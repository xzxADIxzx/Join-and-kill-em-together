namespace Jaket.Input;

using UnityEngine;

using Jaket.Assets;

/// <summary> List of all keybinds of the mod, including serious ones and those that need for a joke. </summary>
public class Keybind
{
    static PrefsManager pm => PrefsManager.Instance;

    /// <summary> List itself. </summary>
    public static Keybind

    LobbyTab   = new("lobby-tab",            KeyCode.F1),
    PlayerList = new("player-list",          KeyCode.F2),
    Settings   = new("settings",             KeyCode.F3),
    PlayerInds = new("player-indicators",    KeyCode.Z),
    PlayerInfo = new("player-information",   KeyCode.X),
    Point      = new("point",                KeyCode.Mouse2),
    Spray      = new("spray",                KeyCode.T),
    EmoteWheel = new("emote-wheel",          KeyCode.B),
    Chat       = new("chat",                 KeyCode.Return),
    ScrollUp   = new("chat-scroll-up",       KeyCode.UpArrow),
    ScrollDown = new("chat-scroll-down",     KeyCode.DownArrow),
    Spectate   = new("spectate",             KeyCode.K),
    SpectNext  = new("spectate-next",        KeyCode.Mouse0),
    SpectPrev  = new("spectate-prev",        KeyCode.Mouse1);

    /// <summary> List of all keybinds used for loading, conflict resolving and so on. </summary>
    public static Keybind[] All = { LobbyTab, PlayerList, Settings, PlayerInds, PlayerInfo, Point, Spray, EmoteWheel, Chat, ScrollUp, ScrollDown, Spectate, SpectNext, SpectPrev };
    /// <summary> List of all keybinds that cannot be assigned to a mouse key. </summary>
    public static Keybind[] Dangerous = { LobbyTab, PlayerList, Settings, Chat };

    /// <summary> Internal name of the keybind. </summary>
    private readonly string name;
    /// <summary> Default value of the keybind that is assigned in the constructor. </summary>
    private readonly KeyCode defaultKey;

    /// <summary> Primary key of the keybind. </summary>
    private KeyCode key;

    private Keybind(string name, KeyCode key)
    {
        this.name = name;
        this.defaultKey = key;
    }

    /// <summary> Returns formatted name of the keybind. </summary>
    public string FormatName() => Bundle.Get($"keybind.{name}", name);

    /// <summary> Returns formatted value of the keybind. </summary>
    public string FormatValue() => key switch
    {
        KeyCode.Mouse0 => "LMB",
        KeyCode.Mouse1 => "RMB",
        KeyCode.Mouse2 => "MMB",
        KeyCode.LeftAlt or KeyCode.RightAlt => "ALT",
        KeyCode.LeftShift or KeyCode.RightShift => "SHIFT",
        KeyCode.LeftControl or KeyCode.RightControl => "CONTROL",
        KeyCode.UpArrow => "UP",
        KeyCode.DownArrow => "DOWN",
        KeyCode.RightArrow => "RIGHT",
        KeyCode.LeftArrow => "LEFT",
        _ => key.ToString().Replace("Alpha", "").Replace("Keypad", "").ToUpper()
    };

    #region state

    /// <summary> Whether the bind is held down. </summary>
    public bool Down() => Input.GetKey(key);

    /// <summary> Whether the bind was just pressed. </summary>
    public bool Tap() => Input.GetKeyDown(key);

    /// <summary> Whether the bind was just released. </summary>
    public bool Release() => Input.GetKeyUp(key);

    #endregion
    #region rebinding

    /// <summary> Saves the bind in preferences. </summary>
    public void Save() => pm.SetInt($"jaket.binds.{name}", (int)key);

    /// <summary> Loads the bind from preferences. </summary>
    public void Load() => key = (KeyCode)pm.GetInt($"jaket.binds.{name}", (int)defaultKey);

    /// <summary> Resets the bind to its default value. </summary>
    public void Reset()
    {
        pm.DeleteKey($"jaket.binds.{name}");
        key = defaultKey;
    }

    /// <summary> Changes the primary key of the bind. </summary>
    public void Rebind(KeyCode key) => this.key = key;

    #endregion
}
