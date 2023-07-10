namespace Jaket.UI;

using UnityEngine;

using Jaket.Net;

public class PlayerList
{
    public static bool Shown;
    private static GameObject canvas, create, invite;

    public static void Update()
    {
        Utils.SetText(create, LobbyController.CreatingLobby ? "CREATING..." : (LobbyController.Lobby == null ? "CREATE LOBBY" : "CLOSE LOBBY"));
        Utils.SetInteractable(invite, LobbyController.Lobby != null);
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
                LobbyController.CloseLobby();
            else
                LobbyController.CreateLobby(Update);

            Update();
        });

        invite = Utils.Button("INVITE FRIEND", canvas.transform, -784f, 332f, () =>
        {
            LobbyController.InviteFriend();
        });

        Update();
    }

    public static void Toggle()
    {
        canvas.SetActive(Shown = !Shown);
        Utils.ToggleCursor(Shown);
    }
}
