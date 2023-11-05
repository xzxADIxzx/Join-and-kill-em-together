namespace Jaket.UI;

using Steamworks.Data;
using System;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

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
        UI.Table("List", transform, 0f, 0f, 640f, 640f, table =>
        {
            UI.Text("--LOBBY BROWSER--", table, 0f, 288f, 640f);

            refresh = UI.Button("REFRESH", table, -227f, 232f, 154f, clicked: Refresh);
            UI.Field("Search", table, 53f, 232f, 374f, 48f, enter: text =>
            {
                search = text.Trim().ToLower();
                Rebuild();
            });

            // add close menu button to the top right corner
            UI.IconButton("X", table, 280f, 232f, new(1f, .2f, .1f), clicked: Toggle);

            // add scroll rect and get the content transform from it
            content = UI.Scroll("List", table, 0f, -56f, 608f, 496f).content;
        });
        Refresh();
    }

    // <summary> Toggles visibility of lobby list. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        // no need to refresh list if we hide it
        if (Shown && transform.childCount > 0) Refresh();
    }

    /// <summary> Rebuilds lobby list to match the list on Steam servers. </summary>
    public void Rebuild()
    {
        refresh.GetComponentInChildren<Text>().text = LobbyController.FetchingLobbies ? "WAIT..." : "REFRESH";

        // destroy old lobby entries if the search is completed
        if (!LobbyController.FetchingLobbies) foreach (Transform child in content) Destroy(child.gameObject);

        // if no lobby is found, then there is no point in adding an empty list
        if (Lobbies == null) return;

        // look for the lobby using the search string
        var lobbies = search == "" ? Lobbies : Array.FindAll(Lobbies, lobby => lobby.GetData("name").ToLower().Contains(search));

        float height = lobbies.Length * 64f;
        content.sizeDelta = new(608f, height);

        float y = height / 2f - 48f / 2f + (64f);
        foreach (var lobby in lobbies)
        {
            var name = " " + lobby.GetData("name");
            if (search != "")
            {
                int index = name.ToLower().IndexOf(search);
                name = name.Insert(index, "<color=#FFB31A>");
                name = name.Insert(index + "<color=#FFB31A>".Length + search.Length, "</color>");
            }

            var button = UI.Button(name, content, 0f, y -= 64f, 608f, size: 28, align: TextAnchor.MiddleLeft, clicked: () => LobbyController.JoinLobby(lobby));

            UI.Text($"{lobby.GetData("level")} {lobby.MemberCount}/8 ", button.transform, 0f, 0f, 608f, 48f, UnityEngine.Color.grey, 28, TextAnchor.MiddleRight);
        }
    }

    /// <summary> Updates the list of public lobbies and rebuilds the menu. </summary>
    public void Refresh()
    {
        LobbyController.FetchLobbies(lobbies =>
        {
            this.Lobbies = lobbies;
            Rebuild();
        });
        Rebuild();
    }
}
