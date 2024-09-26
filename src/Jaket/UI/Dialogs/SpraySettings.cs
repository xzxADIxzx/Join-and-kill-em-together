namespace Jaket.UI.Dialogs;

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.Sprays;
using Jaket.World;

using static Pal;
using static Rect;

/// <summary> Global spray settings not related to the lobby. </summary>
public class SpraySettings : CanvasSingleton<SpraySettings>
{
    static PrefsManager pm => PrefsManager.Instance;

    #region general

    // <summary> Name of the currently selected spray. </summary>
    public static string Current
    {
        get => pm.GetString("jaket.sprays.current");
        set => pm.SetString("jaket.sprays.current", value);
    }
    // <summary> Whether sprays are enabled or not. </summary>
    public static bool Enabled;

    #endregion

    /// <summary> List of the loaded sprays, whitelist and blacklist of players. </summary>
    private RectTransform sprays, players;
    /// <summary> Preview of the currently selected spray. </summary>
    private Image preview;

    /// <summary> Loads and applies all settings. </summary>
    public static void Load()
    {
        Enabled = pm.GetBool("jaket.sprays.enabled", true);
        var c = Current;
        SprayManager.CurrentSpray = SprayManager.Loaded.Find(spray => spray.Name == c);
    }

    private void Start()
    {
        UIB.Table("Settings", "#sprays.name", transform, Size(664f, 760f), table =>
        {
            UIB.IconButton("X", table, Icon(304f, 28f), red, clicked: Toggle);

            sprays = UIB.Rect("Sprays", table, new(-164f, -20f, 320f, 720f));
            players = UIB.Rect("Players", table, new(164f, -20f, 320f, 720f));

            preview = UIB.Image("Preview", sprays, Btn(456f) with { Height = 320f }, type: Image.Type.Filled);

            UIB.Button("#sprays.refresh", sprays, Btn(644f), clicked: Refresh);
            UIB.Button("#sprays.open", sprays, Btn(692f), clicked: OpenFolder);

            UIB.Toggle("#sprays.enabled", players, Tgl(696f), 16, _ =>
            {
                pm.SetBool("jaket.sprays.enabled", Enabled = _);
            }).isOn = Enabled;
        });
        Rebuild();
    }

    private void OpenFolder() => Application.OpenURL($"file://{SprayManager.Folder.Replace("\\", "/")}");

    // <summary> Toggles visibility of the spray settings. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideCentralGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        if (Shown && transform.childCount > 0) Rebuild();
        if (!Shown) SprayDistributor.UploadLocal();
    }

    /// <summary> Rebuilds the spray settings to update the list of sprays and players. </summary>
    public void Rebuild()
    {
        #region left side

        for (int i = 3; i < sprays.childCount; i++) Destroy(sprays.GetChild(i).gameObject);
        for (int i = 0; i < Mathf.Min(6, SprayManager.Loaded.Count); i++)
        {
            var spray = SprayManager.Loaded[i];
            var n = " " + spray.ShortName();
            var r = Btn(28f + 48f * i);

            if (spray.Name == SprayManager.CurrentSpray?.Name)
                UIB.Button(n, sprays, r, green, align: TextAnchor.MiddleLeft);
            else if (spray.IsValid())
                UIB.Button(n, sprays, r, align: TextAnchor.MiddleLeft, clicked: () =>
                {
                    SprayManager.SetSpray(spray);
                    Rebuild();

                    Current = spray.Name;
                });
            else
                UIB.Button(n, sprays, r, red, align: TextAnchor.MiddleLeft, clicked: () => Bundle.Hud("sprays.invalid"));
        }
        if (SprayManager.Loaded.Count < 6) UIB.Button("+", sprays, Btn(28f + 48f * SprayManager.Loaded.Count), grey, clicked: OpenFolder);

        preview.sprite = SprayManager.CurrentSpray != null ? SprayManager.CurrentSpray.Sprite : UIB.Checkmark;
        preview.preserveAspect = true;

        #endregion
        #region right side

        for (int i = 1; i < players.childCount; i++) Destroy(players.GetChild(i).gameObject);
        if ((LobbyController.Lobby?.MemberCount ?? 0) <= 1)
        {
            UIB.Text("#sprays.alone", players, Size(320f, 48f), grey);
            return;
        }

        List<Friend> whitelist = new(), blacklist = new();
        foreach (var member in LobbyController.Lobby?.Members) (Administration.BannedSprays.Contains(member.Id.AccountId) ? blacklist : whitelist).Add(member);

        float y = -20f;
        void BuildList(string name, List<Friend> list, Color color, Action<Friend> clicked)
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
            Administration.BannedSprays.Add(member.Id.AccountId);
            Rebuild();
            if (member.IsMe) Bundle.Hud("sprays.blacklist-yourself");
        });
        BuildList("BLACKLIST:", blacklist, red, member =>
        {
            Administration.BannedSprays.Remove(member.Id.AccountId);
            Rebuild();
            if (member.IsMe) Bundle.Hud("sprays.whitelist-yourself");
        });

        #endregion
    }

    /// <summary> Updates the list of the loaded sprays and rebuilds the menu. </summary>
    public void Refresh()
    {
        SprayManager.LoadSprayFiles();
        Rebuild();
    }
}
