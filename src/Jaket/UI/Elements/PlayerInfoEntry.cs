namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net.Types;
using Jaket.UI;

using static Pal;
using static Rect;

/// <summary> Interface element displaying information about the player such as name, health and railgun charge. </summary>
public class PlayerInfoEntry : MonoBehaviour
{
    /// <summary> Player whose information this entry displays. </summary>
    private RemotePlayer player;

    /// <summary> Component containing the name and rail charge. </summary>
    private Text pname, railc;
    /// <summary> Health images that the player directly sees. </summary>
    private RectTransform health, overhealth;

    /// <summary> Creates an entry with the given parent. </summary>
    public static PlayerInfoEntry Build(RemotePlayer player, Transform parent) =>
        UIB.Component<PlayerInfoEntry>(parent.gameObject, entry => entry.player = player);

    private void Start()
    {
        var t = Size(540f - 16f, 40f);
        pname = UIB.Text($"<b>{player.Header.Name}</b>", transform, t, size: 32, align: TextAnchor.UpperLeft);
        railc = UIB.Text("", transform, t, size: 32, align: TextAnchor.UpperRight);

        var h = Size(540f - 16f, 8f) with { y = -16f };
        UIB.Image("Background", transform, h, black);

        health = UIB.Image("Health", transform, h, red).rectTransform;
        overhealth = UIB.Image("Overhealth", transform, h, green).rectTransform;
    }

    private void Update()
    {
        float hp = player.Health;
        int charge = Mathf.Min(10, player.RailCharge);

        pname.color = hp > 0f ? white : red;
        railc.text = $"<b><color=#0080FF>ϟ</color></b>[<color=#0080FF>{new string('I', charge)}</color><color=#003060>{new string('-', 10 - charge)}</color>]";

        health.localScale = new(Mathf.Min(hp / 100f, 1f), 1f, 1f);
        health.localPosition = new(-(1f - health.localScale.x) * 262f, -16f, 0f);

        overhealth.localScale = new(Mathf.Max((hp - 100f) / 100f, 0f), 1f, 1f);
        overhealth.localPosition = new(-(1f - overhealth.localScale.x) * 262f, -16f, 0f);
    }
}
