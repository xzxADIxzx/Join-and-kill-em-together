namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.World;

using static Rect;

/// <summary> Tab responsible for lobby management. </summary>
public class LobbyTab : CanvasSingleton<LobbyTab>
{
    /// <summary> Lobby control buttons. </summary>
    private Button create, invite, copy, accessibility;
    /// <summary> Input field with lobby name. </summary>
    private InputField field;
    /// <summary> Current lobby access level: 0 - private, 1 - friends only, 2 - public. I was too lazy to create an enum. </summary>
    private int lobbyAccessLevel;
    /// <summary> Checkboxes with lobby settings. </summary>
    private Toggle pvp, cheats;

    private void Start()
    {
        Events.OnLobbyAction += Rebuild;

        UIB.Shadow(transform);
        UIB.Table("Lobby Control", transform, Tlw(16f + 144f / 2f, 144f), table =>
        {
            UIB.Text("#lobby-tab.lobby", table, Btn(0f, 24f), size: 32);
            create = UIB.Button("#lobby-tab.create", table, Btn(0f, 68f), clicked: () =>
            {
                if (LobbyController.Lobby == null)
                    // create a new lobby if not already created
                    LobbyController.CreateLobby();
                else
                    // or leave if already connected to a lobby
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = UIB.Button("#lobby-tab.invite", table, Btn(0f, 116f), clicked: LobbyController.InviteFriend);
        });
        UIB.Table("Lobby Codes", transform, Tlw(176f + 192f / 2f, 192f), table =>
        {
            UIB.Text("#lobby-tab.codes", table, Btn(0f, 24f));
            copy = UIB.Button("#lobby-tab.copy", table, Btn(0f, 68f), clicked: LobbyController.CopyCode);
            UIB.Button("#lobby-tab.join", table, Btn(0f, 116f), clicked: LobbyController.JoinByCode);
            UIB.Button("#lobby-tab.list", table, Btn(0f, 164f), clicked: UIOLD.LobbyList.Instance.Toggle);
        });
        UIB.Table("Lobby Config", transform, Tlw(384f + 342f / 2f, 342f), table =>
        {
            UIB.Text("#lobby-tab.config", table, Btn(0f, 24f));

            field = UIB.Field("#lobby-tab.name", table, Tgl(0f, 64f), cons: name => LobbyController.Lobby?.SetData("name", name));
            field.characterLimit = 24;

            accessibility = UIB.Button("#lobby-tab.private", table, Btn(0f, 108f), clicked: () =>
            {
                switch (lobbyAccessLevel = ++lobbyAccessLevel % 3)
                {
                    case 0: LobbyController.Lobby?.SetPrivate(); break;
                    case 1: LobbyController.Lobby?.SetFriendsOnly(); break;
                    case 2: LobbyController.Lobby?.SetPublic(); break;
                }
                Rebuild();
            });

            pvp = UIB.Toggle("#lobby-tab.allow-pvp", table, Tgl(0f, 152f), clicked: allow => LobbyController.Lobby?.SetData("pvp", allow.ToString()));
            cheats = UIB.Toggle("#lobby-tab.allow-cheats", table, Tgl(0f, 192f), clicked: allow => LobbyController.Lobby?.SetData("cheats", allow.ToString()));

            UIB.Text("#lobby-tab.ppp-desc", table, Btn(0f, 247f) with { Height = 62f }, Color.gray, 16);

            UIB.Text("#lobby-tab.ppp-name", table, Btn(0f, 298f), align: TextAnchor.MiddleLeft);
            var PPP = UIB.Text("0PPP", table, Btn(0f, 298f), align: TextAnchor.MiddleRight);

            UIB.Slider("Health Multiplier", table, Btn(0f, 326f) with { Height = 16f }, 16, value =>
            {
                PPP.text = $"{(int)((LobbyController.PPP = value / 8f) * 100)}PPP";
                LobbyController.Lobby?.SetData("ppp", LobbyController.PPP.ToString());
            });
        });

        Rebuild();
        Version.Label(transform);
    }

    /// <summary> Toggles visibility of lobby tab. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

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

        create.GetComponentInChildren<Text>().text = Bundle.Get(LobbyController.CreatingLobby
            ? "lobby-tab.creating"
            : LobbyController.Lobby == null
                ? "lobby-tab.create"
                : LobbyController.IsOwner ? "lobby-tab.close" : "lobby-tab.leave");

        invite.interactable = copy.interactable = LobbyController.Lobby != null;

        accessibility.GetComponentInChildren<Text>().text = Bundle.Get(lobbyAccessLevel switch
        {
            0 => "lobby-tab.private",
            1 => "lobby-tab.fr-only",
            2 => "lobby-tab.public",
            _ => "lobby-tab.default"
        });

        transform.GetChild(3).gameObject.SetActive(LobbyController.Lobby.HasValue && LobbyController.IsOwner);
    }
}