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
        UIB.Table("Teams", "#player-list.team", transform, new(16f + 532f / 2f, -(16f + 166f / 2f), 532f, 166f, new(0f, 1f)), table =>
        {
            UIB.Text("#player-list.info", table, Btn(71f) with { Height = 46f }, size: 16);

            float x = -24f;
            float y = -130f;
            // foreach (Team team in System.Enum.GetValues(typeof(Team))) UIB.TeamButton(team, table, new(x += 64f, y, 56f, 56f, new(0f, 1f)), () =>
            // {
            //     if (team == Team.Pink) {
            //         x = -24f;
            //         y = -30;
            //     }

            //     Networking.LocalPlayer.Team = team;
            //     Events.OnTeamChanged.Fire();

            //     Rebuild();
            // });

            for (int i = 0; i <= Tools.EnumMax<Team>(); ++i) {
                Team team = (Team)i;

                // if (team == Team.Pink + 1) {
                //     x = -24f;
                //     y = -194;
                // }

                UIB.TeamButton(team, table, new(x += 64f, y, 56f, 56f, new(0f, 1f)), () => {
                    Networking.LocalPlayer.Team = team;
                    Events.OnTeamChanged.Fire();

                    Rebuild();
                });
            }
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
        UIB.Table("List", "#player-list.list", transform, new(16f + 336f / 2f, -(198f + height / 2f), 546f, height, new(0f, 1f)), table =>
        {
            void Msg(string msg) => Chat.Instance.Receive($"[14]{msg}[]");

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
                    UIB.ProfileButton(member, table, Stn(y += 48f, -48f));
                    if (member.Id.AccountId != Tools.AccId && LobbyController.IsOwner)
                    {
                        UIB.IconButton("K", table, Icon(140f, y), new(1f, .8f, 0f), clicked: () => Administration.Kick(member.Id.AccountId));
                        UIB.IconButton("B", table, Icon(188f, y), orange, clicked: () => Administration.Ban(member.Id.AccountId));
                    }
                }

                if (member.Id.AccountId != Tools.AccId) UIB.IconButton("P", table, Icon(236f, y), red, clicked: () => Msg(Tools.ChatStr(Administration.BlacklistAddUID(member.Id.AccountId.ToString()))));
            }
        });
    }
}
