namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net.Types;
using Jaket.UI;

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
        UI.Component<PlayerInfoEntry>(parent.gameObject, entry => entry.player = player);

    private void Start()
    {
        pname = UI.Text($"<b>{player.Header.Name}</b>", transform, 0f, -8f, 524f, align: TextAnchor.UpperLeft);
        railc = UI.Text("", transform, 0f, -8f, 524f, align: TextAnchor.UpperRight);

        UI.Image("Health Background", transform, 0f, -20f, 524f, 8f, Color.black);
        health = UI.Image("Health", transform, 0f, 0f, 524f, 8f, Color.red).rectTransform;
        overhealth = UI.Image("Overhealth", transform, 0f, 0f, 524f, 8f, Color.green).rectTransform;
    }

    private void Update()
    {
        float hp = player.Health;

        pname.color = hp > 0f ? Color.white : Color.red;
        railc.text = $"<color=#D8D8D8>[<color=#0080FF>{new string('I', player.RailCharge)}</color><color=#cccccccc>{new string('-', 10 - player.RailCharge)}</color>]</color>";

        health.localScale = new(Mathf.Min(hp / 100f, 1f), 1f, 1f);
        health.localPosition = new(-(1f - health.localScale.x) * 262f, -20f, 0f);

        overhealth.localScale = new(Mathf.Max((hp - 100f) / 100f, 0f), 1f, 1f);
        overhealth.localPosition = new(-(1f - overhealth.localScale.x) * 262f, -20f, 0f);
    }
}
