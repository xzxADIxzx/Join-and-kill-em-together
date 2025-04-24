namespace Jaket.UI.Dialogs;

using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Lib;
using Jaket.World;

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
        Bar(920f, 520f, b =>
        {
            b.Setup(true);
            b.Text("#lobby-list.name", 32f, 32);

            b.Subbar(40f, s =>
            {
                s.Setup(false, 0f);
                refresh = s.TextButton("", spc: 256f, callback: Refresh);
                s.Field("#lobby-list.search", t =>
                {
                    search = t.Trim().ToLower();
                    Rebuild();
                }, 640f);
            });

            b.Subbar(424f, s =>
            {
                s.Setup(false, 0f);
                content = Component<Bar>(s.ScrollV(0f, 856f).content.gameObject, b => b.Setup(true, 0f));
                s.Slider(content.transform);
            });
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        if (Shown)
        {
            UI.Hide(UI.MidlGroup, this);
            Refresh();
        }
        Movement.UpdateState();
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
            var name = " " + Bundle.CutColors(l.GetData("name"));
            if (!empty)
            {
                int s = name.ToLower().IndexOf(search),
                    e = search.Length + s;

                name = Bundle.Parse($"{name[..s]}<color={Orange}>{name[s..e]}</color>{name[e..]}");
            }

            var cont = content.TextButton(name, align: TextAnchor.MiddleLeft, callback: () => LobbyController.JoinLobby(l));
            var rect = Builder.Rect("Info", cont.transform, Lib.Rect.Fill);

            var full = l.MemberCount <= 2 ? Green : l.MemberCount <= 4 ? Orange : Red;
            var info = $"<color={Gray}>{l.GetData("level")}</color> <color={full}>{l.MemberCount}/{l.MaxMembers}</color> ";

            Builder.Text(rect, info, 24, white, TextAnchor.MiddleRight);
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
