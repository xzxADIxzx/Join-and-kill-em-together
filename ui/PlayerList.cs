namespace Jaket.UI;

using Steamworks;
using UnityEngine;

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

        Utils.Text("--PLAYERS--", list.transform, -784f, 172f);

        float y = 172f;
        LobbyController.EachMember(member => Utils.Button(member.Name, list.transform, -784f, y -= 80f, () => SteamFriends.OpenUserOverlay(member.Id, "steamid")));
    }

    public static void Build()
    {
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
