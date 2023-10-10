namespace Jaket.UI;

using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

/// <summary> Tab responsible for lobby management. </summary>
public class LobbyTab : CanvasSingleton<LobbyTab>
{
    /// <summary> Lobby control buttons. </summary>
    private Button create, invite, copy, accessibility;
    /// <summary> Current lobby access level: 0 - private, 1 - friends only, 2 - public. I was too lazy to create an enum. </summary>
    private int lobbyAccessLevel;

    private void Start()
    {
        UI.Shadow("Shadow", transform);
        UI.TableAT("Lobby Control", transform, 0f, 352f, 192f, table =>
        {
            UI.Text("--LOBBY--", table, 0f, 64f);
            create = UI.Button("CREATE LOBBY", table, 0f, 8f, clicked: () =>
            {
                if (LobbyController.Lobby == null)
                    // create a new lobby if not already created
                    LobbyController.CreateLobby(Rebuild);
                else
                    // or leave if already connected to a lobby
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = UI.Button("INVITE FRIEND", table, 0f, -56f, clicked: LobbyController.InviteFriend);
        });
        UI.TableAT("Lobby Codes", transform, 208f, 352f, 256f, table =>
        {
            UI.Text("--CONNECTION--", table, 0f, 96f);
            copy = UI.Button("COPY LOBBY CODE", table, 0f, 40f, clicked: LobbyController.CopyCode);
            UI.Button("JOIN BY CODE", table, 0f, -24f, clicked: LobbyController.JoinByCode);
            UI.Button("BROWSE PUBLIC LOBBIES", table, 0f, -88f);
        });
        UI.TableAT("Lobby Config", transform, 480f, 352f, 128f, table =>
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

        Rebuild();
    }

    /// <summary> Toggles visibility of lobby tab. </summary>
    public void Toggle()
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown) return;

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

        invite.interactable = copy.interactable = LobbyController.Lobby != null;

        accessibility.GetComponentInChildren<Text>().text = lobbyAccessLevel switch
        {
            0 => "PRIVATE",
            1 => "FRIENDS ONLY",
            2 => "PUBLIC",
            _ => "UNKNOWN"
        };

        transform.GetChild(3).gameObject.SetActive(LobbyController.Lobby.HasValue && LobbyController.IsOwner);
    }
}
