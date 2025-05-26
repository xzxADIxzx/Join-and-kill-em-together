namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for options and keybinds. </summary>
public class Settings : Fragment
{
    static PrefsManager pm => PrefsManager.Instance;

    #region options

    /// <summary> Identifier of the selected localization. </summary>
    public static string Locale
    {
        get => pm.GetString("jaket.locale", "en");
        set => pm.SetString("jaket.locale", value);
    }
    /// <summary> Color of the feedbacker: default, green or blue. </summary>
    public static int FeedColor
    {
        get => pm.GetInt("jaket.feed-color");
        set => pm.SetInt("jaket.feed-color", value % 3);
    }
    /// <summary> Color of the knuckleblaster: default, green or red. </summary>
    public static int KnklColor
    {
        get => pm.GetInt("jaket.knkl-color");
        set => pm.SetInt("jaket.knkl-color", value % 3);
    }
    /// <summary> Whether freeze frames aka hitstops are disabled. </summary>
    public static bool DisableFreezeFrames
    {
        get => pm.GetBool("jaket.disable-freeze", true);
        set => pm.SetBool("jaket.disable-freeze", value);
    }
    // <summary> Percentage volume of the Sam's voice. </summary>
    public static int Volume
    {
        get => pm.GetInt("jaket.tts.volume", 60);
        set
        {
            ModAssets.Mixer?.SetFloat("volume", value / 2f - 30f); // the value should be between -30 and 20 decibels
            pm.SetInt("jaket.tts.volume", value);
        }
    }
    /// <summary> Whether auto text-to-speech is enabled. </summary>
    public static bool AutoTTS
    {
        get => pm.GetBool("jaket.tts.auto");
        set => pm.SetBool("jaket.tts.auto", value);
    }

    #endregion

    /// <summary> Buttons that controls the language and colors of the feedbacker and knuckleblaster. </summary>
    private Button lang, feed, knkl;
    /// <summary> Content of the keybind list. </summary>
    private Bar keylist;

    /// <summary> Keybind that is being reassigned at the moment. </summary>
    public Keybind Rebinding;

    public Settings(Transform root) : base(root, "Settings", true)
    {
        Bar(352f, b =>
        {
            b.Setup(true);
            b.Text("#settings.general", 32f, 32);

            b.FillButton("#settings.reset", red, ResetGeneral);
            b.Separator();

            lang = b.TextButton("", callback: () =>
            {
                Locale = Bundle.Codes[(Bundle.Codes.IndexOf(Locale) + 1) % Bundle.Codes.Length];
                Rebuild();
            });

            feed = b.OffsetButton("FEEDBACKER:", () =>
            {
                FeedColor++;
                Rebuild();
            });

            knkl = b.OffsetButton("KNUCKLEBLASTER:", () =>
            {
                KnklColor++;
                Rebuild();
            });

            b.Toggle("#settings.freeze", b => DisableFreezeFrames = b);
            b.TextButton("#settings.sprays", callback: () => SpraySettings.Instance.Toggle());
        });
        Bar(552f, b =>
        {
            b.Setup(true);
            b.Text("#settings.controls", 32f, 32);

            b.FillButton("#settings.reset", red, ResetControls);
            b.Separator();

            b.Subbar(424f, s =>
            {
                s.Setup(false, 0f);
                keylist = Component<Bar>(s.ScrollV(664f, 384f).content.gameObject, b => b.Setup(true, 0f));
                s.Slider(keylist.transform);
            });
        });
        VersionBar();
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.LeftGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        static string Mode(int mode) => Bundle.Get(mode switch
        {
            0 => "settings.default",
            1 => "settings.green",
            2 => "settings.vanilla",
            _ => "lobby-tab.default"
        });

        lang.GetComponentInChildren<Text>().text = Bundle.Locales[Bundle.Codes.IndexOf(Locale)];
        feed.GetComponentInChildren<Text>().text = Mode(FeedColor);
        knkl.GetComponentInChildren<Text>().text = Mode(KnklColor);

        // update the toggle of the freeze frames
        Content.GetChild(0).GetChild(0).GetChild(6).GetComponent<Toggle>().isOn = DisableFreezeFrames;

        // update the colors of the feedbacker and knuckleblaster
        Events.OnHandChange.Fire();

        keylist.Clear();
        Keybind.All.Each(bind => keylist.RebindButton(bind, () => Rebind(bind)));
    }

    #region load & reset

    /// <summary> Loads and applies all settings. </summary>
    public static void Load()
    {
        Keybind.All.Each(b => b.Load());
        Volume = Volume;
    }

    /// <summary> Resets options. </summary>
    private void ResetGeneral()
    {
        Locale = Bundle.Codes[Bundle.Loaded];
        pm.DeleteKey("jaket.feed-color");
        pm.DeleteKey("jaket.knkl-color");
        pm.DeleteKey("jaket.disable-freeze");
        Rebuild();
    }

    /// <summary> Resets keybinds. </summary>
    private void ResetControls()
    {
        Keybind.All.Each(b => b.Reset());
        Rebuild();
    }

    #endregion
    #region rebinding

    /// <summary> Starts the process of rebinding. </summary>
    public void Rebind(Keybind bind)
    {
        Rebinding = bind;
        Rebuild();
    }

    /// <summary> Checks whether any key is pressed. </summary>
    public void RebindUpdate()
    {
        var current = Event.current;
        if (current.isKey || current.isMouse)
        {
            if (current.isMouse && Keybind.Dangerous.Any(b => b == Rebinding)) return;

            Events.Post(() => Rebinding = null);
            Events.Post(Rebuild);

            if (current.keyCode == KeyCode.Escape) return;

            var key = current.isKey
                ? current.keyCode
                : current.isMouse
                    ? KeyCode.Mouse0 + current.button
                    : KeyCode.None;

            if (key == KeyCode.None) return;

            Rebinding.Rebind(key);
            Rebinding.Save();
        }
    }

    #endregion
}
