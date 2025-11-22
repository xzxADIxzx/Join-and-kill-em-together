namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Admin;
using Jaket.Sprays;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for spray options and blacklist. </summary>
public class SpraySettings : Fragment
{
    static PrefsManager pm => PrefsManager.Instance;

    #region options

    /// <summary> Spray chosen by the player. </summary>
    public static string Current
    {
        get => pm.GetString("jaket.sprays.current");
        set => pm.SetString("jaket.sprays.current", value);
    }
    /// <summary> Whether sprays are enabled. </summary>
    public static bool Enabled
    {
        get => pm.GetBool("jaket.sprays.enabled");
        set => pm.SetBool("jaket.sprays.enabled", value);
    }

    #endregion

    /// <summary> Preview of the currently selected image. </summary>
    private Image preview;
    /// <summary> Bars displaying different spray options. </summary>
    private Bar loaded, hidden;

    public SpraySettings(Transform root) : base(root, "SpraySettings", true)
    {
        Bar(888f, 920f, b =>
        {
            b.Setup(true);
            b.Text("#spray.name", 32f, 32);

            b.Subbar(864f, s =>
            {
                s.Setup(false, 0f);
                s.Subbar(432f, b =>
                {
                    b.Setup(true, 0f);

                    preview = b.Image(null, 432f, white, ImageType.Filled);
                    preview.preserveAspect = true;

                    b.Subbar(328f, s =>
                    {
                        s.Setup(false, 0f);
                        loaded = Component<Bar>(s.ScrollV(0f, 384f).content.gameObject, b => b.Setup(true, 0f));
                        s.Slider(loaded.transform);
                    });

                    b.TextButton("#spray.refresh", callback: Refresh);
                    b.TextButton("#spray.open", callback: OpenFolder);
                });
                s.Subbar(432f, b => (hidden = b).Setup(true, 0f));
            });
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.MidlGroup, this, Rebuild);

        if (!Shown && SprayManager.Uploaded != SprayManager.Selected && SprayManager.Selected != null) SprayManager.UploadLocal();
    }

    public override void Rebuild()
    {
        preview.sprite = SprayManager.Selected != null ? SprayManager.Selected.Sprite : Tex.Mark;

        loaded.Clear();
        hidden.Clear();

        (loaded.transform as RectTransform).pivot = new(.5f, 1f);
        (loaded.transform as RectTransform).sizeDelta = new(384f, SprayManager.Local.Count * 48f - 8f);

        SprayManager.Local.Each(s =>
        {
            if (s.Name == Current)
                loaded.FillButton(s.Short, green, () => { });
            else if (s.Valid)
                loaded.TextButton(s.Short, light, () =>
                {
                    SprayManager.Selected = s;
                    Current = s.Name;
                    Rebuild();
                });
            else
                loaded.TextButton(s.Short, red, () => Bundle.Hud("spray.toobig"));
        });

        hidden.Toggle("#spray.show", b => Enabled = b).isOn = Enabled;

        foreach (var blacklist in new bool[] { false, true })
        {
            if (LobbyController.Lobby?.Members.All(m => Administration.Hidden.Contains(m.AccId) != blacklist || m.IsMe) ?? true) continue;

            var color = blacklist ? red : green;

            hidden.Resolve("Space", 16f);
            hidden.Text(blacklist ? "#spray.blacklist" : "#spray.whitelist", 24f, 24, color, TextAnchor.MiddleLeft);

            LobbyController.Lobby?.Members.Each(m => Administration.Hidden.Contains(m.AccId) == blacklist && !m.IsMe, m => hidden.TextButton(m.Name, color, () =>
            {
                if (blacklist)
                    Administration.Hidden.Remove(m.AccId);
                else
                    Administration.Hidden.Add(m.AccId);

                Rebuild();
            }));
        }
    }

    public void Refresh() { SprayManager.Load(); Rebuild(); }

    public void OpenFolder() => Application.OpenURL($"file://{Files.Sprays.Replace("\\", "/")}");
}
