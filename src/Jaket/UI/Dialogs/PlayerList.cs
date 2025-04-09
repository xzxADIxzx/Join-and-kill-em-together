namespace Jaket.UI.Dialogs;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI.Lib;
using Jaket.World;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that displays all players and teams. </summary>
public class PlayerList : Fragment
{
    public PlayerList(Transform root) : base(root, "PlayerList", true) => Events.OnTeamChange += () => { if (Shown) Rebuild(); };

    public override void Toggle()
    {
        base.Toggle();
        if (Shown)
        {
            UI.Hide(UI.LeftGroup, this);
            Rebuild();
        }
        Movement.UpdateState();
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
                for (Team i = Team.Yellow; i <= Team.Pink; i++) s.TeamButton(i, () =>
                {
                    Networking.LocalPlayer.Team = i;
                    Events.OnTeamChange.Fire();
                });
            });
        });
        VersionBar();
    }
}
