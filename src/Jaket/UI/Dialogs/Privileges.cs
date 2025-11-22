namespace Jaket.UI.Dialogs;

using UnityEngine;

using Jaket.Net;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that displays the list of privileged lobby members. </summary>
public class Privileges : Fragment
{
    /// <summary> Bars displaying their member groups. </summary>
    private Bar lower, upper;

    public Privileges(Transform root) : base(root, "Privileges", true)
    {
        Events.OnLobbyAction += () => { if (Shown) Rebuild(); };

        Bar(888f, 512f, b =>
        {
            b.Setup(true);
            b.Text("#privileges.name", 32f, 32);

            b.Subbar(416f, s =>
            {
                s.Setup(false, 0f);
                s.Subbar(432f, b => (lower = b).Setup(true, 0f));
                s.Subbar(432f, b => (upper = b).Setup(true, 0f));
            });
            b.Text("#privileges.warn", 32f, 16, light, TextAnchor.MiddleLeft);
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.MidlGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        lower.Clear();
        upper.Clear();

        foreach (var level in new Bar[] { lower, upper })
        {
            var upper = level == this.upper;

            if (LobbyController.Lobby?.Members.All(m => LobbyConfig.Privileged.Any(p => p == m.AccId.ToString()) != upper) ?? true) continue;

            var color = upper ? green : red;

            level.Text(upper ? "#privileges.upper" : "#privileges.lower", 24f, 24, color, TextAnchor.MiddleLeft);

            LobbyController.Lobby?.Members.Each(m => LobbyConfig.Privileged.Any(p => p == m.AccId.ToString()) == upper, m => level.TextButton(m.Name, color, () =>
            {
                if (upper)
                    LobbyConfig.Set("privileged", LobbyConfig.Get("privileged").Replace(m.AccId.ToString(), string.Empty).Replace("  ", string.Empty).Trim());
                else
                    LobbyConfig.Set("privileged", LobbyConfig.Get("privileged") + $" {m.AccId}");
            }));
        }
    }
}
