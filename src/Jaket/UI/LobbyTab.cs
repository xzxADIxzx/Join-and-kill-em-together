namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.World;

/// <summary> Tab responsible for lobby management. </summary>
public class LobbyTab : CanvasSingleton<LobbyTab>
{
    /// <summary> Lobby control buttons. </summary>
    private Button create, invite, copy, accessibility;
    private Text inLobbyNotice;
    private Image lobbyControlTable;
    /// <summary> Input field with lobby name. </summary>
    private InputField field;
    /// <summary> Current lobby access level: 0 - private, 1 - friends only, 2 - public. I was too lazy to create an enum. </summary>
    private int lobbyAccessLevel;
    /// <summary> Checkboxes with lobby settings. </summary>
    private Toggle pvp, cheats;

    private void Start()
    {
        Events.OnLobbyAction += Rebuild;

        UI.Shadow("Shadow", transform);
        lobbyControlTable = UI.TableAT("Lobby Control", transform, 0f, 352f, 192f, table =>
        {
            UI.Text("--LOBBY--", table, 0f, 64f);
            create = UI.Button("CREATE LOBBY", table, 0f, 8f, clicked: () =>
            {
                if (LobbyController.Lobby == null)
                    // create a new lobby if not already created
                    LobbyController.CreateLobby();
                else
                    // or leave if already connected to a lobby
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = UI.Button("INVITE FRIEND", table, 0f, -56f, clicked: LobbyController.InviteFriend);
        });
        inLobbyNotice = UI.Text("You can't create lobbies in the main menu.\nPlease, enter any mission to able to create a lobby.", transform, -768f, 480f, 320f, 64f, Color.gray, 16);
        UI.TableAT("Lobby Codes", transform, 208f, 352f, 256f, table =>
        {
            UI.Text("--CONNECTION--", table, 0f, 96f);
            copy = UI.Button("COPY LOBBY CODE", table, 0f, 40f, clicked: LobbyController.CopyCode);
            UI.Button("JOIN BY CODE", table, 0f, -24f, clicked: LobbyController.JoinByCode);
            UI.Button("BROWSE PUBLIC LOBBIES", table, 0f, -88f, size: 24, clicked: LobbyList.Instance.Toggle);
        });
        UI.TableAT("Lobby Config", transform, 480f, 352f, 430f, table =>
        {
            UI.Text("--CONFIG--", table, 0f, 183f);

            field = UI.Field("Lobby name", table, 0f, 135f, 320f, enter: name => LobbyController.Lobby?.SetData("name", name));
            field.characterLimit = 24;

            accessibility = UI.Button("PRIVATE", table, 0f, 79f, clicked: () =>
            {
                switch (lobbyAccessLevel = ++lobbyAccessLevel % 3)
                {
                    case 0: LobbyController.Lobby?.SetPrivate(); break;
                    case 1: LobbyController.Lobby?.SetFriendsOnly(); break;
                    case 2: LobbyController.Lobby?.SetPublic(); break;
                }
                Rebuild();
            });

            pvp = UI.Toggle("ALLOW PvP", table, 0f, 23f, clicked: allow => LobbyController.Lobby?.SetData("pvp", allow.ToString()));
            cheats = UI.Toggle("ALLOW CHEATS", table, 0f, -25f, clicked: allow => LobbyController.Lobby?.SetData("cheats", allow.ToString()));

            UI.Text("Percentage per player is the number of percentages that will be added to the boss's health for each player starting from the second",
                    table, 0f, -88f, height: 62f, color: Color.gray, size: 16);

            UI.Text("BOSS HP:", table, 0f, -151f, align: TextAnchor.MiddleLeft);
            var PPP = UI.Text("0PPP", table, 0f, -151f, align: TextAnchor.MiddleRight);

            UI.Slider("Health Multiplier", table, 0f, -191f, 320f, 16f, 16, value =>
            {
                PPP.text = $"{(int)((LobbyController.PPP = value / 8f) * 100)}PPP";
                LobbyController.Lobby?.SetData("ppp", LobbyController.PPP.ToString());
            });
        });

        Rebuild();
        Version.Label(transform);
        WidescreenFix.MoveDown(transform);
    }

    /// <summary> Toggles visibility of lobby tab. </summary>
    public void Toggle()
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown && !LobbyList.Shown) return;

        gameObject.SetActive(Shown = !Shown);
        // no need to update state if main menu, because it can capture mouse
        if (Tools.Scene != "Main Menu") Movement.UpdateState();

        // no need to update tab if we hide it
        if (Shown && transform.childCount > 0) Rebuild();
    }

    /// <summary> Rebuilds lobby tab to update control buttons. </summary>
    public void Rebuild()
    {
        // reset config
        if (LobbyController.Lobby == null)
        {
            lobbyAccessLevel = 0;
            pvp.isOn = cheats.isOn = true;
        }
        else field.text = LobbyController.Lobby?.GetData("name");

        create.GetComponentInChildren<Text>().text = LobbyController.CreatingLobby
            ? "CREATING..."
            : LobbyController.Lobby == null
                ? "CREATE LOBBY"
                : LobbyController.IsOwner ? "CLOSE LOBBY" : "LEAVE LOBBY";

        invite.interactable = copy.interactable = LobbyController.Lobby != null;
        
        var IsMainMenu = Tools.Scene == "Main Menu";
        // hide lobby controls if main menu is shown
        lobbyControlTable.gameObject.SetActive(!IsMainMenu);
        inLobbyNotice.gameObject.SetActive(IsMainMenu);

        accessibility.GetComponentInChildren<Text>().text = lobbyAccessLevel switch
        {
            0 => "PRIVATE",
            1 => "FRIENDS ONLY",
            2 => "PUBLIC",
            _ => "UNKNOWN"
        };

        // disabling lobby config controls if lobby is not created
        transform.GetChild(4).gameObject.SetActive(LobbyController.Lobby.HasValue && LobbyController.IsOwner);
    }
}
