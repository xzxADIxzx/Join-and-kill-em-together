namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.IO;
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
        SprayManager.Local.Each(s =>
        {
            var name = " " + Bundle.CutColors(s.Short);

            if (s.Name == Current)
                loaded.FillButton(name, green, () => { });
            else if (s.Valid)
                loaded.TextButton(name, light, () =>
                {
                    SprayManager.Selected = s;
                    Current = s.Name;
                    Rebuild();
                });
            else
                loaded.TextButton(name, red, () => Bundle.Hud("spray.toobig"));
        });
        /*
        #region right side

        for (int i = 1; i < players.childCount; i++) Dest(players.GetChild(i).gameObject);
        if ((LobbyController.Lobby?.MemberCount ?? 0) <= 1)
        {
            UIB.Text("#sprays.alone", players, Size(320f, 48f), gray);
            return;
        }

        List<Friend> whitelist = new(), blacklist = new();
        foreach (var member in LobbyController.Lobby?.Members) (Administration.Hidden.Contains(member.Id.AccountId) ? blacklist : whitelist).Add(member);

        float y = -20f;
        void BuildList(string name, List<Friend> list, Color color, Cons<Friend> clicked)
        {
            if (list.Count == 0) return;
            UIB.Text(name, players, Btn(y += 48f), align: TextAnchor.MiddleLeft);

            foreach (var member in list)
            {
                var sucks = member;
                UIB.Button(member.Name, players, Btn(y += 48f), color, clicked: () => clicked(sucks));
            }
        }
        BuildList("WHITELIST:", whitelist, green, member =>
        {
            Administration.Hidden.Add(member.Id.AccountId);
            Rebuild();
            if (member.IsMe) Bundle.Hud("sprays.blacklist-yourself");
        });
        BuildList("BLACKLIST:", blacklist, red, member =>
        {
            Administration.Hidden.Remove(member.Id.AccountId);
            Rebuild();
            if (member.IsMe) Bundle.Hud("sprays.whitelist-yourself");
        });

        #endregion
        */
    }

    public void Refresh() { SprayManager.Load(); Rebuild(); }

    public void OpenFolder() => Application.OpenURL($"file://{Files.Sprays.Replace("\\", "/")}");
}
