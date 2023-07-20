namespace Jaket.UI;

using Steamworks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.Net;

/// <summary> List of all players and lobby controller. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class PlayerList : MonoSingleton<PlayerList>
{
    /// <summary> Whether player list is visible or hidden. </summary>
    public bool Shown;

    /// <summary> Lobby control buttons. </summary>
    private GameObject createButton, inviteButton;
    /// <summary> List of players itself. </summary>
    private GameObject list;

    /// <summary> Creates a singleton of player list. </summary>
    public static void Build()
    {
        // initialize the singleton and create a canvas
        Utils.Canvas("Player List", Plugin.Instance.transform).AddComponent<PlayerList>();

        // hide player list once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => Instance.gameObject.SetActive(Instance.Shown = false);

        // build player list
        Utils.Shadow("Shadow", Instance.transform, -810f, 0f);
        Utils.Text("--LOBBY--", Instance.transform, -784f, 492f);

        // create lobby control buttons
        Instance.createButton = Utils.Button("", Instance.transform, -784f, 412f, () =>
        {
            if (LobbyController.Lobby == null)
                // create a new lobby if not already created
                LobbyController.CreateLobby(Instance.Rebuild);
            else
                // or leave if already connected to a lobby
                LobbyController.LeaveLobby();

            Instance.Rebuild();
        });

        Instance.inviteButton = Utils.Button("INVITE FRIEND", Instance.transform, -784f, 332f, LobbyController.InviteFriend);

        float x = -986f;
        foreach (Team team in Enum.GetValues(typeof(Team))) Utils.TeamButton("", Instance.transform, x += 67f, 252f, team.Data().Color(), () =>
        {
            // change player team
            Networking.LocalPlayer.team = team;

            // update player indicators to only show teammates & player list to display new team
            PlayerIndicators.Instance.Rebuild();
            Instance.Rebuild();
        });

        // create a rect transform in which there will be a players list
        Instance.list = Utils.Rect("List", Instance.transform, 0f, 0f, 1920f, 1080f);
        Instance.Rebuild();
    }

    /// <summary> Toggles visibility of indicators. </summary>
    public void Toggle()
    {
        // if the player is typing, then nothing needs to be done
        if (Chat.Shown) return;

        // no comments
        gameObject.SetActive(Shown = !Shown);
        Utils.ToggleCursor(Shown);

        // no need to update list if we hide it
        if (Shown) Rebuild();
    }

    /// <summary> Rebuilds player list to add new players or remove players left the lobby. </summary>
    public void Rebuild()
    {
        // update buttons based on current state
        var text = LobbyController.CreatingLobby ? "CREATING..." :
                   LobbyController.Lobby == null ? "CREATE LOBBY" :
                   LobbyController.IsOwner ? "CLOSE LOBBY" : "LEAVE LOBBY";

        Utils.SetText(createButton, text);
        Utils.SetInteractable(inviteButton, LobbyController.Lobby != null);

        // clear the players list
        foreach (Transform child in list.transform) Destroy(child.gameObject);

        // if the player is not in the lobby, then there is no point in adding an empty player list
        if (LobbyController.Lobby == null) return;

        Utils.Text("--PLAYERS--", list.transform, -784f, 92f);

        float y = 92f;
        LobbyController.EachMember(member =>
        {
            // paint the nickname in the team color
            var team = member.IsMe ? Networking.LocalPlayer.team : (Networking.Players.TryGetValue(member.Id, out var player) ? player.team : Team.Yellow);
            var name = $"<color=#{ColorUtility.ToHtmlStringRGBA(team.Data().Color())}>{member.Name}</color>";

            // add a button with a nickname forwarding to the player's profile
            Utils.Button(name, list.transform, -784f, y -= 80f, () => SteamFriends.OpenUserOverlay(member.Id, "steamid"));
        });
    }
}
