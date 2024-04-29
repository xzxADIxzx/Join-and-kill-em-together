namespace Jaket.UI.Dialogs;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.World;

using static Pal;
using static Rect;

/// <summary> List of all players and teams. </summary>
public class PlayerList : CanvasSingleton<PlayerList>
{
    private void Start()
    {
        UIB.Shadow(transform);
        UIB.Table("Teams", "#player-list.team", transform, Tlw(16f + 170f / 2f, 170f), table =>
        {
            UIB.Text("#player-list.info", table, Btn(0f, 73f) with { Height = 50f }, size: 17);

            float x = -24f;
            foreach (Team team in System.Enum.GetValues(typeof(Team))) UIB.TeamButton(team, table, Tlw(134f, 56f) with { x = x += 64f, Width = 56f }, () =>
            {
                Networking.LocalPlayer.Team = team;
                Events.OnTeamChanged.Fire();

                Rebuild();
            });
        });

        Version.Label(transform);
        Rebuild();
    }

    // <summary> Toggles visibility of the player list. </summary>
    public void Toggle()
    {
        if (!Shown) UI.HideLeftGroup();

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        if (Shown && transform.childCount > 0) Rebuild();
    }

    /// <summary> Rebuilds the player list to add new players or remove players left the lobby. </summary>
    public void Rebuild()
    {
        // destroy old player list
        if (transform.childCount > 3) Destroy(transform.GetChild(3).gameObject);
        if (LobbyController.Offline) return;

        float height = LobbyController.Lobby.Value.MemberCount * 48f + 48f;
        UIB.Table("List", "#player-list.list", transform, Tlw(200f + height / 2f, height), table =>
        {
            float y = 20f;
            foreach (var member in LobbyController.Lobby?.Members)
            {
                if (LobbyController.LastOwner == member.Id)
                {
                    UIB.ProfileButton(member, table, Btn(-24f, y += 48f) with { Width = 272f });
                    UIB.IconButton("★", table, Icon(138f, y), new(1f, .7f, .1f), new(1f, 4f), () => Bundle.Hud("player-list.owner"));
                }
                else
                {
                    if (LobbyController.IsOwner)
                    {
                        UIB.ProfileButton(member, table, Btn(-24f, y += 48f) with { Width = 272f });
                        UIB.IconButton("X", table, Icon(138f, y), red, clicked: () => Administration.Ban(member.Id.AccountId));
                    }
                    else UIB.ProfileButton(member, table, Btn(0f, y += 48f));
                }
            }
        });
    }
}
