namespace Jaket.UI.Dialogs;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for lobby management. </summary>
public class LobbyTab : Fragment
{
    /// <summary> Buttons that control the lobby and its config. </summary>
    private Button create, invite, copy, join, list, accessibility;
    /// <summary> Toggles that control the lobby and its config. </summary>
    private Toggle pvp, mod, bosses;
    /// <summary> Sliders that control the lobby and its config. </summary>
    private Slider ppp;

    /// <summary> Field that displays the name of the lobby. </summary>
    private InputField name;
    /// <summary> Current access level of the lobby: private, friends only or public. </summary>
    private int accessLevel;

    public LobbyTab(Transform root) : base(root, "LobbyTab", true)
    {
        Events.OnLobbyAction += () => { if (Shown) Rebuild(); };

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
            list = b.TextButton("#lobby-tab.list", callback: () => UI.LobbyList.Toggle());
        });
        Bar(518f, b =>
        {
            b.Setup(true);
            b.Text("#lobby-tab.config", 32f, 32);

            name = b.Field("#lobby-tab.name", s => LobbyConfig.Name = s);
            name.characterLimit = 30;

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

            pvp = b.Toggle("#lobby-tab.allow-pvp", b => LobbyConfig.PvPAllowed = b);
            mod = b.Toggle("#lobby-tab.allow-mod", b => LobbyConfig.ModsAllowed = b);

            b.Text("#lobby-tab.ppp-desc", 46f, 16, light, TextAnchor.MiddleLeft).alignByGeometry = true;
            ppp = b.Slider(0, 16, i => LobbyConfig.PPP = i, "#lobby-tab.ppp-name", i => $"{(int)(i / 8f * 100f)}PPP");

            bosses = b.Toggle("#lobby-tab.heal-bosses", b => LobbyConfig.HealBosses = b);

            b.Separator();
            b.TextButton("#lobby-tab.gamemode", red, () => { });
            b.TextButton("#lobby-tab.cheats",   red, () => { });
        });
        VersionBar();
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.LeftGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        create.GetComponentInChildren<Text>().text = Bundle.Get
        (
            LobbyController.Creating
            ? "lobby-tab.creating" :
            LobbyController.Offline
            ? "lobby-tab.create" :
            LobbyController.IsOwner
            ? "lobby-tab.close"
            : "lobby-tab.leave"
        );
        invite.interactable = copy.interactable = LobbyController.Online;

        // the third bar shouldn't be visible at all if the lobby is null
        Sidebar.transform.GetChild(2).gameObject.SetActive(LobbyController.Online);

        if (LobbyController.Offline)
        {
            accessLevel = 0;
            return;
        }

        name.text = LobbyConfig.Name;
        accessibility.GetComponentInChildren<Text>().text = Bundle.Get((LobbyController.IsOwner ? accessLevel : -1) switch
        {
            0 => "lobby-tab.private",
            1 => "lobby-tab.fr-only",
            2 => "lobby-tab.public",
            _ => "lobby-tab.default"
        });
        name.interactable = accessibility.interactable = LobbyController.IsOwner;

        pvp.isOn = LobbyConfig.PvPAllowed;
        mod.isOn = LobbyConfig.ModsAllowed;
        bosses.isOn = LobbyConfig.HealBosses;
        ppp.value = LobbyConfig.PPP * 8f;

        foreach (var toggle in new Selectable[] { pvp, mod, bosses, ppp })
        {
            toggle.interactable = LobbyController.IsOwner;
            toggle.GetComponentInChildren<Image>().color = LobbyController.IsOwner ? white : dark;
        }
    }
}
