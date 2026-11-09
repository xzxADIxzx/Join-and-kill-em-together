namespace Jaket.UI.Dialogs;

using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI.Lib;
using Jaket.World;

using static Jaket.UI.Lib.Pal;

/// <summary> Dialog that is responsible for gamemode configuration. </summary>
public class GameConfig : Fragment
{
    /// <summary> Gamemode whose preview card is selected. </summary>
    private Gamemode selected;
    /// <summary> Scrollable container with all the cards. </summary>
    private Bar scroll;
    /// <summary> Information about the selected gamemode. </summary>
    private Text info;
    /// <summary> Checkboxes of gamemode modifiers & such. </summary>
    private Toggle slowmo, hammer, bleedy;

    /// <summary> Preview cards. </summary>
    private Image[] cards = new Image[Gamemodes.All.Length];

    public GameConfig(Transform root) : base(root, "GameConfig", true)
    {
        Events.OnLobbyAction += () => { if (Shown) Rebuild(); };

        Bar(556f, 716f, b =>
        {
            b.Setup(true);
            b.Title("#gameconfig.name");

            scroll = b.ScrollH(Gamemodes.All.Length * 228f - 8f, 300f).content.Add<Bar>(s =>
            {
                s.Setup(false, 0f);
                Events.Post(() => ModAssets.CardIcons.All(t => t != null), () => Gamemodes.All.Each(m =>
                {
                    var img = cards[(byte)m] = s.Image(ModAssets.CardIcons[(byte)m], 220f);
                    var btn = Builder.Rect("Mode", img, new()           );
                    var txt = Builder.Rect("Text", btn, new() { Y = 8f });

                    Builder.Button(btn, Tex.BrdL, white, () =>
                    {
                        selected = m;
                        Rebuild();
                    });
                    Builder.Text(txt, $"#gameconfig.titles.No{(byte)m}", 42, white, TextAnchor.LowerCenter);
                }));
            });

            info = b.Text("", 21, light, TextAnchor.UpperLeft, 200f);
            b.Text("#gameconfig.mods");

            slowmo = b.Toggle("#gameconfig.slowmo", b => LobbyConfig.Slowmo = b);
            hammer = b.Toggle("#gameconfig.hammer", b => LobbyConfig.Hammer = b);
            bleedy = b.Toggle("#gameconfig.bleedy", b => LobbyConfig.Bleedy = b);
        });
    }

    public override void Toggle()
    {
        base.Toggle();
        UI.Hide(UI.MidlGroup, this, () =>
        {
            selected = Gameflow.Mode;
            Rebuild();

            scroll.transform.localPosition = Vector3.left * 228f * (Gamemodes.All.IndexOf(selected) - Gamemodes.All.Length / 2);
        });
        if (LobbyController.IsOwner && selected != Gameflow.Mode)
            LobbyConfig.Mode = selected.ToString().ToLower();
    }

    public override void Rebuild()
    {
        StringBuilder builder = new("\n\n");

        if (selected.PvP            ()) builder.Append(Bundle.Get("gameconfig.totals.pvp"));
        if (selected.HPs            ()) builder.Append(Bundle.Get("gameconfig.totals.hps"));
        if (selected.HealOnKill     ()) builder.Append(Bundle.Get("gameconfig.totals.hok"));
        if (selected.NoCommonEnemies()) builder.Append(Bundle.Get("gameconfig.totals.nce"));
        if (selected.WaveLikeEnemies()) builder.Append(Bundle.Get("gameconfig.totals.wle"));

        info.text = Bundle.Format($"gameconfig.briefs.No{(byte)selected}", builder.ToString());

        slowmo.isOn = LobbyConfig.Slowmo;
        hammer.isOn = LobbyConfig.Hammer;
        bleedy.isOn = LobbyConfig.Bleedy;

        slowmo.interactable = hammer.interactable = bleedy.interactable = LobbyController.IsOwner;

        cards.Each(c => c.color = white);
        cards[(byte)selected].color = red;
    }
}
