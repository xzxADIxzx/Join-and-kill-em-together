namespace Jaket.UI.Dialogs;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Admin;
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
        Bar(168f, b =>
        {
            b.Setup(true);
            b.Title("#player-list.team");

            b.Info("#player-list.info", 64f);
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
        if (LobbyController.Online) Bar(LobbyController.Lobby.Value.MemberCount * 48f + (LobbyController.IsOwner ? 120f : 48f), b =>
        {
            b.Setup(true);
            b.Title("#player-list.list");

            LobbyController.Lobby?.Members.Each(m => b.Subbar(40f, s =>
            {
                s.Setup(false, 0f);
                if (LobbyController.Owner == m.AccId)
                {
                    s.TeamButton(m, 384f);
                    s.FillButton(ModAssets.LobbyOwner, yellow, () => Bundle.Hud("player-list.owner"));
                }
                else if (LobbyController.IsOwner)
                {
                    s.TeamButton(m, 384f);
                    s.FillButton(ModAssets.LobbyBan, red, () => Administration.Ban(m.AccId));
                }
                else s.TeamButton(m, 432f);
            }));
            if (!LobbyController.IsOwner) return;

            b.Separator();
            b.FillButton("#player-list.clear", red, () => LobbyConfig.Banned = []);
        });
        VersionBar();
    }
}
