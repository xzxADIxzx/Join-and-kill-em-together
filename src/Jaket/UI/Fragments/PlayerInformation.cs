namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that displays the state of all teammates. </summary>
public class PlayerInformation : Fragment
{
    static HudController hc => HudController.Instance;
    static ColorBlindSettings cb => ColorBlindSettings.Instance;

    public PlayerInformation(Transform root) : base(root, "PlayerInformation", false, cond: () => Scene == "Main Menu")
    {
        Events.OnLobbyEnter += () => { if (!Shown) Toggle(); };
        Events.OnTeamChange += Rebuild;
    }

    public override void Toggle()
    {
        base.Toggle();
        this.Rebuild();
    }

    public override void Rebuild()
    {
        Dest(hc.transform.Find("Info")?.gameObject);
        if (Shown) Build();
    }

    public void Build() => Builder.WorldCanvas(Create("Info", hc.transform).transform, default, c =>
    {
        Component<HUDPos>(c.gameObject, p =>
        {
            c.localPosition = new(1.06f, -.53f, 1f);
            c.localRotation = Quaternion.Euler(0f, 30f, 0f);
            c.localScale = new(.0007f, .001f, .001f);

            p.reversePos = new(-1.06f, -.53f, 1f);
            p.reverseRot = new(0f, 330f, 0f);
            p.active = true;
        });
        c.gameObject.layer = 13; // always on top

        var number = Networking.Entities.Count(e => e is RemotePlayer p && p.Team.Ally());
        var height = number == 0 ? 184f : 32f + 80f * number;

        var root = Builder.Image(Builder.Rect("Root", c, new(97f, -224f + height / 2, 777f, height)), Tex.Fill, black, ImageType.Sliced, 1.35f);
        Component<Bar>(root.gameObject, b =>
        {
            b.Setup(true, 20f);
            b.Update(() => root.color = black with { a = PrefsManager.Instance.GetFloat("hudBackgroundOpacity") / 100f });

            if (number == 0)
                Builder.Text(Builder.Rect("Text", b.Image(Tex.Back, 144f, purple, multiplier: 2f).transform, Lib.Rect.Fill), "#playerinfo", 32, white, TextAnchor.MiddleCenter);
            else
                Networking.Entities.Player(p => p.Team.Ally(), p => Build(p, b.Resolve("Entry", 72f)));
        });
        Events.Post2(() => root.GetComponentsInChildren<Graphic>().Each(g => g.material = hc.fistFill.material));
    });

    public void Build(RemotePlayer player, Transform root)
    {
        RectTransform Slider(Color color) => Builder.Image(Builder.Rect("Slider", root, Lib.Rect.Fill), Tex.Fill, color, ImageType.Sliced, 2f).rectTransform;
        RectTransform
            background = Slider(black with { a = .69f }),
            normhealth = Slider(cb.healthBarColor),
            overhealth = Slider(cb.overHealColor);

        Text Text(TextAnchor align) => Builder.Text(Builder.Rect("Text", root, Lib.Rect.Fill with { Width = -32f }), "", 40, white, align);
        Text
            playername = Text(TextAnchor.MiddleLeft),
            railcharge = Text(TextAnchor.MiddleRight);

        Component<Bar>(root.gameObject, b => b.Update(() =>
        {
            int health = player.Health, charge = Mathf.Min(player.Charge, 8);

            playername.text = $"<color={(health > 0 ? White : Red)}>{player.Header.Name}</color>";
            railcharge.text = $"<color={Charge}><b>ϟ</b>[<i>{new string('▪', charge)}<color={Empty}>{new string('▫', 8 - charge)}</color></i>]</color>";

            normhealth.sizeDelta = new(health <= 000 ? -737f : -7.37f * Mathf.Clamp(100 - health, 0, 96), 0f);
            overhealth.sizeDelta = new(health <= 100 ? -737f : -7.37f * Mathf.Clamp(200 - health, 0, 96), 0f);

            normhealth.anchoredPosition = new(normhealth.sizeDelta.x / 2f, 0f);
            overhealth.anchoredPosition = new(overhealth.sizeDelta.x / 2f, 0f);
        }));
    }
}
