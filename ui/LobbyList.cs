namespace Jaket.UI;

using Steamworks.Data;
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
    /// <summary> Content of the lobby list. </summary>
    private RectTransform content;

    private void Start()
    {
        UI.Table("List", transform, 0f, 0f, 640f, 640f, table =>
        {
            UI.Text("--LOBBY BROWSER--", table, 0f, 288f, 640f);

            refresh = UI.Button("REFRESH", table, -227f, 232f, 154f, clicked: () =>
            {
                LobbyController.FetchLobbies(lobbies =>
                {
                    this.Lobbies = lobbies;
                    Rebuild();
                });
                Rebuild();
            });
            UI.Field("Search", table, 53f, 232f, 374f, 48f, enter: text => { /* TODO */ });

            // add close menu button to the top right corner
            UI.Button("X", table, 280f, 232f, 48f, 48f, new(1f, .2f, .1f), 40, clicked: Toggle).transform.GetChild(0).localPosition = new(.5f, 2.5f, 0f);

            // add scroll rect and get the content transform from it
            content = UI.Scroll("List", table, 0f, -56f, 608f, 496f).content;
        });
        Rebuild();
    }

    // <summary> Toggles visibility of lobby list. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();
    }

    /// <summary> Rebuilds lobby list to match the list on Steam servers. </summary>
    public void Rebuild()
    {
        refresh.GetComponentInChildren<Text>().text = LobbyController.FetchingLobbies ? "WAIT..." : "REFRESH";

        // destroy old lobby entries if the search is completed
        if (!LobbyController.FetchingLobbies) foreach (Transform child in content) Destroy(child.gameObject);

        // if no lobby is found, then there is no point in adding an empty list
        if (Lobbies == null) return;

        float height = Lobbies.Length * 64f;
        content.sizeDelta = new(608f, height);

        float y = height / 2f - 48f / 2f + (64f);
        foreach (var lobby in Lobbies)
        {
            // fetch lobby's name & other data
            lobby.Refresh();

            var button = UI.Button(" " + lobby.GetData("name"), content, 0f, y -= 64f, 608f,
                size: 28, align: TextAnchor.MiddleLeft, clicked: () => LobbyController.JoinLobby(lobby)).transform;

            UI.Text($"{lobby.GetData("level")} {lobby.MemberCount}/8 ", button, 0f, 0f, 608f, 48f, UnityEngine.Color.grey, 28, TextAnchor.MiddleRight);
        }
    }
}
