namespace Jaket.UI.Dialogs;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.World;

using static Pal;
using static Rect;

public class TeamList : CanvasSingleton<TeamList>
{
    public static float Height => Minimal ? 294f : 166f + 64f * Mathf.Ceil((float)Team.Count / 5f);
    private static bool Minimal => !LobbyController.IsLobbyMultikill && LobbyController.Online;

    private void Start()
    {
        Rebuild();
    }

    // <summary> Toggles visibility of the team list. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = !Shown);
        if (Shown && transform.childCount > 0) Rebuild();
    }

    public void Rebuild()
    {
        // destroy old team list
        if (transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);

        UIB.Table("Teams", "#player-list.team", transform, Tlw(16f + Height / 2f, Height), table =>
        {
            UIB.Text("#player-list.info", table, Btn(71f) with { Height = 46f }, size: 16);

            float x = -24f;
            float y = -106f;

            foreach (Team team in System.Enum.GetValues(typeof(Team)))
            {
                if (team == Team.Count) continue; // this isn't actually a team, so skip it
                if (Minimal && team > Team.Pink + 1) continue;
                if ((int)team % 5 == 0) // start a new row
                {
                    x = -24;
                    y -= 64f;
                }

                var usedTeam = Minimal && team > Team.Pink ? Team.White : team;

                UIB.TeamButton(usedTeam, table, new(x += 64f, y, 56f, 56f, new(0f, 1f)), () =>
                {
                    Networking.LocalPlayer.Team = usedTeam;
                    Events.OnTeamChanged.Fire();

                    PlayerList.Instance.Rebuild();
                });
            }
        });
    }
}

/// <summary> List of all players and teams. </summary>
public class PlayerList : CanvasSingleton<PlayerList>
{
    private void Start()
    {
        UIB.Shadow(transform);
        Version.Label(transform);
        Rebuild();
    }

    // <summary> Toggles visibility of the player list. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideLeftGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();
        TeamList.Instance.Toggle();

        if (Shown && transform.childCount > 0) Rebuild();
    }

    /// <summary> Rebuilds the player list to add new players or remove players left the lobby. </summary>
    public void Rebuild()
    {
        // destroy old player list
        if (transform.childCount > 2) Destroy(transform.GetChild(2).gameObject);
        if (LobbyController.Offline) return;

        float height = LobbyController.Lobby.Value.MemberCount * 48f + 48f;
        UIB.Table("List", "#player-list.list", transform, Tlw(TeamList.Height + 16f + height / 2f, height), table =>
        {
            float y = 20f;
            foreach (var member in LobbyController.Lobby?.Members)
            {
                if (LobbyController.LastOwner == member.Id)
                {
                    UIB.ProfileButton(member, table, Stn(y += 48f, -48f));
                    UIB.IconButton("★", table, Icon(140f, y), new(1f, .7f, .1f), new(1f, 4f), () => Bundle.Hud("player-list.owner"));
                }
                else
                {
                    if (LobbyController.IsOwner)
                    {
                        UIB.ProfileButton(member, table, Stn(y += 48f, -96f));
                        UIB.IconButton("X", table, Icon(96f, y),  red,    clicked: () => Administration.Ban(member.Id.AccountId));
                        UIB.IconButton("Y", table, Icon(140f, y), yellow, clicked: () => Administration.Kick(member.Id.AccountId));
                    }
                    else UIB.ProfileButton(member, table, Btn(y += 48f));
                }
            }
        });
    }
}
