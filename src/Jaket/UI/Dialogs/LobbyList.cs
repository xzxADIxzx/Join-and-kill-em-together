namespace Jaket.UI.Dialogs;

using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.World;

using static Pal;
using static Rect;

/// <summary> Browser for public lobbies that receives the list via Steam API and displays it in the scrollbar. </summary>
public class LobbyList : CanvasSingleton<LobbyList>
{
    /// <summary> List of lobbies currently displayed. </summary>
    public Lobby[] Lobbies;
    /// <summary> Button that updates the lobby list. </summary>
    private Button refresh;
    /// <summary> String by which the lobby will be searched. </summary>
    private string search = "";
    /// <summary> Content of the lobby list. </summary>
    private RectTransform content;

    private void Start()
    {
        UIB.Table("List", "#lobby-list.name", transform, Size(640f, 640f), table =>
        {
            refresh = UIB.Button("", table, new(100f, -68f, 184f, 40f, new(0f, 1f)), clicked: Refresh);
            UIB.Field("#lobby-list.search", table, new(392f, -68f, 384f, 40f, new(0f, 1f)), cons: text =>
            {
                search = text.Trim().ToLower();
                Rebuild();
            });

            UIB.IconButton("X", table, Icon(292f, 68f), red, clicked: Toggle);
            content = UIB.Scroll("List", table, new(0f, 272f, 624f, 544f, new(.5f, 0f), new(.5f, 0f))).content;
        });
        Refresh();
    }

    // <summary> Toggles visibility of the lobby list. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideCentralGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        if (Shown && transform.childCount > 0) Refresh();
    }

    /// <summary> Rebuilds the lobby list to match the list on Steam servers. </summary>
    public void Rebuild()
    {
        refresh.GetComponentInChildren<Text>().text = Bundle.Get(LobbyController.FetchingLobbies ? "lobby-list.wait" : "lobby-list.refresh");

        // destroy old lobby entries if the search is completed
        if (!LobbyController.FetchingLobbies) foreach (Transform child in content) Dest(child.gameObject);
        if (Lobbies == null) return;

        // look for the lobby using the search string
        var lobbies = search == "" ? Lobbies : Array.FindAll(Lobbies, lobby => lobby.GetData("name").ToLower().Contains(search));

        float height = lobbies.Length * 48;
        content.sizeDelta = new(624f, height);

        float y = -24f;
        foreach (var lobby in lobbies)
        {
            string level = lobby.GetData("level");

            // hide non-jaket lobbies
            if (!LobbyController.IsJaketLobby(lobby)) continue;

            // hide lobbies the player was banned from
            if (lobby.GetData("banned").Contains(AccId.ToString())) continue;

            // this can happen if the host of a lobby crashes in a very specific way
            // these lobbies can't be joined, so they also get hidden
            if (level == "Main Menu") continue;

            var name = " " + lobby.GetData("name");
            var r = Btn(y += 48f) with { Width = 624f };

            if (search != "")
            {
                int index = name.ToLower().IndexOf(search);
                name = name.Insert(index, "<color=#FFA500>");
                name = name.Insert(index + "<color=#FFA500>".Length + search.Length, "</color>");
            }

            var b = UIB.Button(name, content, r, align: TextAnchor.MiddleLeft, clicked: () => LobbyController.JoinLobby(lobby));

            var full = lobby.MemberCount <= 2 ? Green : lobby.MemberCount <= 4 ? Orange : Red;
            var info = $"<color=#BBBBBB>{LobbyController.MapMap(level)}</color> <color={full}>{lobby.MemberCount}/{lobby.MaxMembers}</color> ";
            UIB.Text(info, b.transform, r.Text, align: TextAnchor.MiddleRight);
        }
    }

    /// <summary> Updates the list of public lobbies and rebuilds the menu. </summary>
    public void Refresh()
    {
        LobbyController.FetchLobbies(lobbies =>
        {
            Lobbies = lobbies;
            Rebuild();
        });
        Rebuild();
    }
}
