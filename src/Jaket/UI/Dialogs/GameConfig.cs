namespace Jaket.UI.Dialogs;

using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Content;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for gamemode configuration. </summary>
public class GameConfig : Fragment
{
    /// <summary> Gamemode whose preview card is selected. </summary>
    private Gamemode selected;
    /// <summary> Information about the selected gamemode. </summary>
    private Text info;

    public GameConfig(Transform root) : base(root, "GameConfig", true)
    {
        Bar(556f, 776f, b =>
        {
            b.Setup(true);
            b.Text("#gameconfig.name", 32f, 32);

            Component<Bar>(b.ScrollH(Gamemodes.All.Length * 228f - 8f, 300f).content.gameObject, s =>
            {
                s.Setup(false, 0f);
                Events.Post(() => ModAssets.CardIcons.All(t => t != null), () => Gamemodes.All.Each(m =>
                {
                    var img = s.Image(ModAssets.CardIcons[(byte)m], 220f).transform;
                    var btn = Builder.Rect("Mode", img, Lib.Rect.Fill                );
                    var txt = Builder.Rect("Text", btn, Lib.Rect.Fill with { Y = 8f });

                    Builder.Button(btn, Tex.Large, white, () =>
                    {
                        selected = m;
                        Rebuild();
                    });
                    Builder.Text(txt, $"#gameconfig.titles.No{(byte)m}", 42, white, TextAnchor.LowerCenter);
                }));
            });

            info = b.Text("keen eye", 300f, align: TextAnchor.UpperLeft, color: light);
            b.Text("#gameconfig.mods", 24f, align: TextAnchor.MiddleLeft);

            b.Toggle("#gameconfig.slowmo", b => { });
            b.Toggle("#gameconfig.hammer", b => { });
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.MidlGroup, this, Rebuild);
    }

    public override void Rebuild()
    {
        StringBuilder builder = new();

        if (selected.PvP            ()) builder.Append(Bundle.Get("gameconfig.totals.pvp"));
        if (selected.HPs            ()) builder.Append(Bundle.Get("gameconfig.totals.hps"));
        if (selected.HealOnKill     ()) builder.Append(Bundle.Get("gameconfig.totals.hok"));
        if (selected.NoCommonEnemies()) builder.Append(Bundle.Get("gameconfig.totals.nce"));
        if (selected.WaveLikeEnemies()) builder.Append(Bundle.Get("gameconfig.totals.wle"));

        info.text = Bundle.Format($"gameconfig.briefs.No{(byte)selected}", builder.ToString());
    }
}
