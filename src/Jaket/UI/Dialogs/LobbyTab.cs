namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Lib;
using Jaket.World;

/// <summary> Dialog that is responsible for lobby management. </summary>
public class LobbyTab : Fragment
{
    /// <summary> Buttons that control the lobby and its config. </summary>
    private Button create, invite, copy, join, list, accessibility;
    /// <summary> Toggles that control the lobby and its config. </summary>
    private Toggle pvp, cheats, mods, bosses;

    /// <summary> Field that displays the name of the lobby. </summary>
    private InputField name;
    /// <summary> Current access level of the lobby: private, friends only or public. </summary>
    private int accessLevel;

    public LobbyTab(Transform root) : base(root, "LobbyTab", true)
    {
        Events.OnLobbyAction += Rebuild;

        Bar(144f, b =>
        {
            b.Setup(true);
            b.Text("#lobby-tab.lobby", 32f, 32);

            create = b.TextButton("#lobby-tab.create", callback: () =>
            {
                if (LobbyController.Offline)
                    LobbyController.CreateLobby();
                else
                    LobbyController.LeaveLobby();

                Rebuild();
            });
            invite = b.TextButton("#lobby-tab.invite", callback: LobbyController.InviteFriend);
        });
        Bar(192f, b =>
        {
            b.Setup(true);
            b.Text("#lobby-tab.codes", 32f, 32);

            copy = b.TextButton("#lobby-tab.copy", callback: LobbyController.CopyCode);
            join = b.TextButton("#lobby-tab.join", callback: LobbyController.JoinByCode);
            list = b.TextButton("#lobby-tab.list", callback: LobbyList.Instance.Toggle);
        });
        Bar(416f, b =>
        {
            b.Setup(true);
            b.Text("#lobby-tab.config", 32f, 32);

            name = b.Field("#lobby-tab.name", s => LobbyController.Lobby?.SetData("name", s));
            name.characterLimit = 32; // TODO adjust

            accessibility = b.TextButton("", callback: () =>
            {
                switch (accessLevel = ++accessLevel % 3)
                {
                    case 0: LobbyController.Lobby?.SetPrivate();     break;
                    case 1: LobbyController.Lobby?.SetFriendsOnly(); break;
                    case 2: LobbyController.Lobby?.SetPublic();      break;
                }
                Rebuild();
            });

            pvp    = b.Toggle("#lobby-tab.allow-pvp",    b => LobbyController.Lobby?.SetData("pvp",    b.ToString()));
            cheats = b.Toggle("#lobby-tab.allow-cheats", b => LobbyController.Lobby?.SetData("cheats", b.ToString()));
            mods   = b.Toggle("#lobby-tab.allow-mods",   b => LobbyController.Lobby?.SetData("mods",   b.ToString()));

            b.Text("#lobby-tab.ppp-desc", 64f, 16);
            b.Slider(0, 16, i =>
            {
                LobbyController.PPP = i / 8f;
                LobbyController.Lobby?.SetData("ppp", i.ToString());

            }, "#lobby-tab.ppp-name", i => $"{(int)(i / 8f * 100f)}PPP");

            bosses = b.Toggle("#lobby-tab.heal-bosses", b => LobbyController.Lobby?.SetData("heal-bosses", b.ToString()));
        });

        // Version.Label(Content); TODO update the version class
        Rebuild();
    }

    public override void Toggle() // TODO update UI hide
    {
        if (!Shown) UI.HideLeftGroup();

        base.Toggle();
        Movement.UpdateState();

        if (Shown) Rebuild();
    }

    public override void Rebuild()
    {
        if (LobbyController.Offline)
        {
            accessLevel = 0;
            pvp.isOn = true;
            cheats.isOn = false;
            mods.isOn = false;
            bosses.isOn = true;
        }
        else name.text = LobbyController.Lobby?.GetData("name");

        create.GetComponentInChildren<Text>().text = Bundle.Get(LobbyController.CreatingLobby
            ? "lobby-tab.creating"
            : LobbyController.Offline
                ? "lobby-tab.create"
                : LobbyController.IsOwner ? "lobby-tab.close" : "lobby-tab.leave");

        invite.interactable = copy.interactable = LobbyController.Online;

        accessibility.GetComponentInChildren<Text>().text = Bundle.Get(accessLevel switch
        {
            0 => "lobby-tab.private",
            1 => "lobby-tab.fr-only",
            2 => "lobby-tab.public",
            _ => "lobby-tab.default"
        });

        // TODO make everything unusable instead
        Content.GetChild(3).gameObject.SetActive(LobbyController.Online && LobbyController.IsOwner);
    }
}
