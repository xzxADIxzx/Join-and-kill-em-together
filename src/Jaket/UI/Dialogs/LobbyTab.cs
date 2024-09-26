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
    private Toggle pvp, cheats, mods, bosses;

    private void Start()
    {
        Events.OnLobbyAction += Rebuild;

        UIB.Shadow(transform);
        UIB.Table("Lobby Control", "#lobby-tab.lobby", transform, Tlw(16f + 144f / 2f, 144f), table =>
        {
            create = UIB.Button("#lobby-tab.create", table, Btn(68f), clicked: () =>
            {
                if (LobbyController.Offline)
                    // create a new lobby if not already created
                    LobbyController.CreateLobby();
                else
                    // or leave if already connected to a lobby
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = UIB.Button("#lobby-tab.invite", table, Btn(116f), clicked: LobbyController.InviteFriend);
        });
        UIB.Table("Lobby Codes", "#lobby-tab.codes", transform, Tlw(176f + 192f / 2f, 192f), table =>
        {
            copy = UIB.Button("#lobby-tab.copy", table, Btn(68f), clicked: LobbyController.CopyCode);
            UIB.Button("#lobby-tab.join", table, Btn(116f), clicked: LobbyController.JoinByCode);
            UIB.Button("#lobby-tab.list", table, Btn(164f), clicked: LobbyList.Instance.Toggle);
        });
        UIB.Table("Lobby Config", "#lobby-tab.config", transform, Tlw(384f + 422f / 2f, 422f), table =>
        {
            field = UIB.Field("#lobby-tab.name", table, Tgl(64f), cons: name => LobbyController.Lobby?.SetData("name", name));
            field.characterLimit = 28;

            accessibility = UIB.Button("#lobby-tab.private", table, Btn(108f), clicked: () =>
            {
                switch (lobbyAccessLevel = ++lobbyAccessLevel % 3)
                {
                    case 0: LobbyController.Lobby?.SetPrivate(); break;
                    case 1: LobbyController.Lobby?.SetFriendsOnly(); break;
                    case 2: LobbyController.Lobby?.SetPublic(); break;
                }
                Rebuild();
            });

            pvp = UIB.Toggle("#lobby-tab.allow-pvp", table, Tgl(152f), clicked: allow => LobbyController.Lobby?.SetData("pvp", allow.ToString()));
            cheats = UIB.Toggle("#lobby-tab.allow-cheats", table, Tgl(192f), clicked: allow => LobbyController.Lobby?.SetData("cheats", allow.ToString()));
            mods = UIB.Toggle("#lobby-tab.allow-mods", table, Tgl(232f), clicked: allow => LobbyController.Lobby?.SetData("mods", allow.ToString()));

            UIB.Text("#lobby-tab.ppp-desc", table, Btn(287f) with { Height = 62f }, size: 16);

            UIB.Text("#lobby-tab.ppp-name", table, Btn(338f), align: TextAnchor.MiddleLeft);
            var PPP = UIB.Text("0PPP", table, Btn(338f), align: TextAnchor.MiddleRight);

            UIB.Slider("Health Multiplier", table, Sld(366f), 16, value =>
            {
                PPP.text = $"{(int)((LobbyController.PPP = value / 8f) * 100)}PPP";
                LobbyController.Lobby?.SetData("ppp", LobbyController.PPP.ToString());
            });

            bosses = UIB.Toggle("#lobby-tab.heal-bosses", table, Tgl(398f), 20, allow => LobbyController.Lobby?.SetData("heal-bosses", allow.ToString()));
        });

        Version.Label(transform);
        Rebuild();
    }

    /// <summary> Toggles visibility of the lobby tab. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideLeftGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        if (Shown && transform.childCount > 0) Rebuild();
    }

    /// <summary> Rebuilds the lobby tab to update control buttons. </summary>
    public void Rebuild()
    {
        // reset config
        if (LobbyController.Offline)
        {
            lobbyAccessLevel = 0;
            pvp.isOn = true;
            cheats.isOn = false;
            mods.isOn = false;
            bosses.isOn = true;
        }
        else field.text = LobbyController.Lobby?.GetData("name");

        create.GetComponentInChildren<Text>().text = Bundle.Get(LobbyController.CreatingLobby
            ? "lobby-tab.creating"
            : LobbyController.Offline
                ? "lobby-tab.create"
                : LobbyController.IsOwner ? "lobby-tab.close" : "lobby-tab.leave");

        invite.interactable = copy.interactable = LobbyController.Online;

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
