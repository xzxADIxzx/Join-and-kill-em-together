namespace Jaket.UI.Dialogs;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that displays all players and teams. </summary>
public class PlayerList : Fragment
{
    public PlayerList(Transform root) : base(root, "PlayerList", true) => Events.OnTeamChange += () =>
    {
        if (Shown)
        {
            Rebuild();
            Sidebar.transform.Each(c => c.localScale = Vector3.one);
        }
    };

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.LeftGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        Sidebar?.Clear();
        Bar(166f, b =>
        {
            b.Setup(true);
            b.Text("#player-list.team", 32f, 32);

            b.Text("#player-list.info", 62f, 16, light, TextAnchor.MiddleLeft).alignByGeometry = true;
            b.Subbar(40f, s =>
            {
                s.Setup(false, 0f);
                Teams.All.Each(t => s.TeamButton(t, () =>
                {
                    Networking.LocalPlayer.Team = t;
                    Events.OnTeamChange.Fire();
                }));
            });
        });
        if (LobbyController.Online) Bar(120f + (LobbyController.Lobby?.MemberCount ?? 0f) * 48f, b =>
        {
            b.Setup(true);
            b.Text("#player-list.list", 32f, 32);

            LobbyController.Lobby?.Members.Each(m => b.Subbar(40f, s =>
            {
                s.Setup(false, 0f);
                if (LobbyController.LastOwner == m.Id.AccountId)
                {
                    s.ProfileButton(m, false);
                    s.FillButton(ModAssets.LobbyOwner, yellow, () => Bundle.Hud("player-list.owner"));
                }
                else if (LobbyController.IsOwner)
                {
                    s.ProfileButton(m, false);
                    s.FillButton(ModAssets.LobbyBan, red, () => Administration.Ban(m.Id.AccountId));
                }
                else s.ProfileButton(m, true);
            }));
            if (!LobbyController.IsOwner) return;

            b.Separator();
            b.TextButton("#player-list.clear", red, () => LobbyConfig.Banned = new string[0]);
        });
        VersionBar();
    }
}
