namespace Jaket.UI.Dialogs;

using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that displays the list of public lobbies acquired via Steam Matchmaking. </summary>
public class LobbyList : Fragment
{
    /// <summary> List of lobbies received after the last refresh. </summary>
    private Lobby[] lobbies;
    /// <summary> Button that refreshes the list of public lobbies. </summary>
    private Button refresh;
    /// <summary> Content of the search bar. </summary>
    private string search;
    /// <summary> Content of the lobby list. </summary>
    private Bar content;

    public LobbyList(Transform root) : base(root, "LobbyList", true)
    {
        Bar(920f, 528f, b =>
        {
            b.Setup(true);
            b.Title("#lobby-list.name");

            b.Subbar(40f, s =>
            {
                s.Setup(false, 0f);
                refresh = s.TextButton("", Refresh, 256f);
                s.Field("#lobby-list.search", t =>
                {
                    search = t.Trim().ToLower();
                    Rebuild();
                }, 640f);
            });

            b.Subbar(424f, s =>
            {
                s.Setup(false, 0f);
                content = s.ScrollV(0f, 856f).content.Add<Bar>(b => b.Setup(true, 0f));
                s.Slider(content);
            });
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.MidlGroup, this, Refresh);
    }

    public override void Rebuild()
    {
        refresh.GetComponentInChildren<Text>().text = Bundle.Get(LobbyController.Fetching ? "lobby-list.refreshing" : "lobby-list.refresh");
        content.Clear();

        if (lobbies == null) return;

        var empty = string.IsNullOrWhiteSpace(search);
        int count = empty ? lobbies.Length : lobbies.Count(l => l.GetData("name").ToLower().Contains(search));

        (content.transform as RectTransform).pivot = new(.5f, 1f);
        (content.transform as RectTransform).sizeDelta = new(856f, count * 48f - 8f);

        lobbies.Each(l => empty || l.GetData("name").ToLower().Contains(search), l =>
        {
            var name = Bundle.CutColors(l.GetData("name"));
            var level = Bundle.CutColors(l.GetData("level"));
            int count = l.MemberCount, max = l.MaxMembers;

            if (!empty)
            {
                int s = name.ToLower().IndexOf(search);
                int e = s + search.Length;

                name = $"{name[..s]}[orange]{name[s..e]}[]{name[e..]}";
            }
            var info = $"[light]{level}[] [{(count <= 2 ? "green" : count <= 4 ? "yellow" : count <= 6 ? "orange" : "red")}]{count}/{max}";

            var cont = Builder.Button(content.Resolve("Button", 40f), Tex.BrdL, white, () => LobbyController.JoinLobby(l));

            Builder.Text(Builder.Rect("Name", cont, new() { Width = -24f }), Bundle.Parse(name), 24, white, TextAnchor.MiddleLeft);
            Builder.Text(Builder.Rect("Info", cont, new() { Width = -24f }), Bundle.Parse(info), 24, white, TextAnchor.MiddleRight);
        });
    }

    public void Refresh()
    {
        LobbyController.FetchLobbies(l =>
        {
            lobbies = l;
            Rebuild();
        });
        lobbies = null;
        Rebuild();
    }
}
