namespace Jaket.UI;

using Steamworks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Content;
using Jaket.Net;

public class PlayerList
{
    public static bool Shown;
    private static GameObject canvas, create, invite, list;

    public static void Update()
    {
        var text = LobbyController.CreatingLobby ? "CREATING..." : LobbyController.Lobby == null ? "CREATE LOBBY" : LobbyController.IsOwner ? "CLOSE LOBBY" : "LEAVE LOBBY";
        Utils.SetText(create, text);
        Utils.SetInteractable(invite, LobbyController.Lobby != null);

        foreach (Transform child in list.transform) GameObject.Destroy(child.gameObject);
        if (LobbyController.Lobby == null) return;

        Utils.Text("--PLAYERS--", list.transform, -784f, 92f);

        float y = 92f;
        LobbyController.EachMember(member =>
        {
            // paint the nickname in the team color
            var team = member.IsMe ? Networking.LocalPlayer.team : (Networking.Players.TryGetValue(member.Id, out var player) ? player.team : Team.Yellow);
            var name = "<color=#" + ColorUtility.ToHtmlStringRGBA(team.Data().Color()) + ">" + member.Name + "</color>";

            // add a button with a nickname forwarding to the player's profile
            Utils.Button(name, list.transform, -784f, y -= 80f, () => SteamFriends.OpenUserOverlay(member.Id, "steamid"));
        });
    }

    public static void Build()
    {
        // hide player list once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => canvas.SetActive(Shown = false);

        canvas = Utils.Canvas("Player List", Plugin.Instance.transform);
        canvas.SetActive(false);

        Utils.Shadow("Shadow", canvas.transform, -800f, 0f);
        Utils.Text("--LOBBY--", canvas.transform, -784f, 492f);

        create = Utils.Button("CREATE LOBBY", canvas.transform, -784f, 412f, () =>
        {
            if (LobbyController.Lobby != null)
                LobbyController.LeaveLobby();
            else
                LobbyController.CreateLobby(Update);

            Update();
        });

        invite = Utils.Button("INVITE FRIEND", canvas.transform, -784f, 332f, LobbyController.InviteFriend);

        float x = -986f;
        foreach (Team team in Enum.GetValues(typeof(Team))) Utils.TeamButton("Change Team", canvas.transform, x += 67f, 252f, team.Data().Color(), () =>
        {
            Networking.LocalPlayer.team = team;
            Update();

            // update player indicators to only show teammates
            PlayerIndicators.Instance.Rebuild();
        });

        list = Utils.Rect("List", canvas.transform, 0f, 0f, 1920f, 1080f);

        Update();
    }

    public static void Toggle()
    {
        canvas.SetActive(Shown = !Shown);
        Utils.ToggleCursor(Shown);

        // update player list
        if (Shown) Update();
    }
}
