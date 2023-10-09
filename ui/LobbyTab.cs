namespace Jaket.UI;

using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

/// <summary> Tab responsible for lobby management. </summary>
public class LobbyTab : CanvasSingleton<LobbyTab>
{
    /// <summary> Lobby control buttons. </summary>
    private Button create, invite, accessibility;
    /// <summary> Current lobby access level: 0 - private, 1 - friends only, 2 - public. I was too lazy to create an enum. </summary>
    private int lobbyAccessLevel;

    private void Start()
    {
        UI.Shadow("Shadow", transform);
        UI.Table("Lobby Control", transform, -960f + 176f + 16f, 540f - 96f - 16f, 352f, 192f, table =>
        {
            UI.Text("--LOBBY--", table, 0f, 64f);
            create = UI.Button("CREATE LOBBY", table, 0f, 8f, clicked: () =>
            {
                if (LobbyController.Lobby == null)
                    // create a new lobby if not already created
                    LobbyController.CreateLobby(this.Rebuild);
                else
                    // or leave if already connected to a lobby
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = UI.Button("INVITE FRIEND", table, 0f, -56f, clicked: LobbyController.InviteFriend);
        });
        UI.Table("Lobby Config", transform, -768f, 332f - 64f - 16f, 352f, 128f, table =>
        {
            UI.Text("--CONFIG--", table, 0f, 32f);
            accessibility = UI.Button("PRIVATE", table, 0f, -24f, clicked: () =>
            {
                switch (lobbyAccessLevel = ++lobbyAccessLevel % 3)
                {
                    case 0: LobbyController.Lobby?.SetPrivate(); break;
                    case 1: LobbyController.Lobby?.SetFriendsOnly(); break;
                    case 2: LobbyController.Lobby?.SetPublic(); break;
                }
                Rebuild();
            });
        });
    }

    /// <summary> Toggles visibility of lobby tab. </summary>
    public void Toggle()
    {
        // if the player is typing, then nothing needs to be done
        if (Chat.Instance.Shown) return; // TODO UI.Any || Shown

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();
    }

    /// <summary> Rebuilds lobby tab to update control buttons. </summary>
    public void Rebuild()
    {
        create.GetComponentInChildren<Text>().text = LobbyController.CreatingLobby
            ? "CREATING..."
            : LobbyController.Lobby == null
                ? "CREATE LOBBY"
                : LobbyController.IsOwner ? "CLOSE LOBBY" : "LEAVE LOBBY";

        invite.interactable = LobbyController.Lobby != null;

        accessibility.GetComponentInChildren<Text>().text = lobbyAccessLevel switch
        {
            0 => "PRIVATE",
            1 => "FRIENDS ONLY",
            2 => "PUBLIC",
            _ => "UNKNOWN"
        };
    }
}
