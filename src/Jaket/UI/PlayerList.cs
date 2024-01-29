namespace Jaket.UI;

using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.World;

/// <summary> List of all players and teams. </summary>
public class PlayerList : CanvasSingleton<PlayerList>
{
    private void Start()
    {
        UI.Shadow("Shadow", transform);
        UI.TableAT("Teams", transform, 0f, 352f, 204f, table =>
        {
            UI.Text("--TEAMS--", table, 0f, 70f);
            UI.Text("This is a PvP mechanic!\nIf you don't want to hurt each other, join the same team :3", table, 0f, 13f, height: 50f, color: Color.gray, size: 17);

            float x = -352f / 2f + 16f + 58f / 2f - (66f);
            foreach (Team team in System.Enum.GetValues(typeof(Team))) UI.TeamButton(team, table, x += 66f, -57f, clicked: () =>
            {
                Networking.LocalPlayer.Team = team;
                Events.OnTeamChanged.Fire();

                Rebuild();
            });
        });

        Version.Label(transform);
        WidescreenFix.MoveDown(transform);
        Rebuild();
    }

    // <summary> Toggles visibility of player list. </summary>
    public void Toggle()
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown) return;

        gameObject.SetActive(Shown = !Shown);
        Movement.UpdateState();

        // no need to update list if we hide it
        if (Shown && transform.childCount > 0) Rebuild();
    }

    /// <summary> Rebuilds player list to add new players or remove players left the lobby. </summary>
    public void Rebuild()
    {
        // destroy old player list
        if (transform.childCount > 3) Destroy(transform.GetChild(3).gameObject);

        // if the player is not in the lobby, then there is no point in adding an empty player list
        if (LobbyController.Lobby == null) return;

        float height = LobbyController.Lobby.Value.MemberCount * 64f + 64f;
        UI.TableAT("List", transform, 220f + WidescreenFix.Offset, 352f, height, table =>
        {
            UI.Text("--PLAYERS--", table, 0f, height / 2f - 32f);

            float y = height / 2f - 64f - 24f + (64f);
            LobbyController.EachMember(member =>
            {
                if (LobbyController.LastOwner == member.Id)
                {
                    UI.ProfileButton(member, table, -32f, y -= 64f, 256f);
                    UI.IconButton("★", table, 136f, y, new(1f, .7f, .1f), new(.5f, 4f, 0f), () => UI.SendMsg("Lobby owner, your life depends on him :D"));
                }
                else
                {
                    if (LobbyController.IsOwner)
                    {
                        UI.ProfileButton(member, table, -32f, y -= 64f, 256f);
                        UI.IconButton("X", table, 136f, y, new(1f, .2f, .1f), clicked: () => LobbyController.KickMember(member));
                    }
                    else UI.ProfileButton(member, table, 0f, y -= 64f);
                }
            });
        });
    }
}
