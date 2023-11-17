namespace Jaket.UI.Elements;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.UI;

/// <summary> Player-created pointer, disappears in a few seconds after appearing. </summary>
public class Pointer : MonoBehaviour
{
    /// <summary> Pointer color. Usually matches the color of some team. </summary>
    private Color color;
    /// <summary> Pointer position in space. </summary>
    private Vector3 position, direction;

    /// <summary> Transform of the player who created the pointer. </summary>
    private Transform player;
    /// <summary> Line going from the pointer to the player who created the pointer. </summary>
    private LineRenderer line;

    /// <summary> Components from which the pointer is made. </summary>
    private RectTransform circle1, circle2, pointer;
    /// <summary> How many seconds has the pointer existed. </summary>
    public float Lifetime;

    /// <summary> Spawns a pointer at the given position. </summary>
    public static Pointer Spawn(Team team, Vector3 position, Vector3 direction, Transform player = null) =>
        UI.Component<Pointer>(UI.Object("Pointer"), pointer =>
        {
            pointer.color = team.Data().Color();
            pointer.color.a = .85f;

            pointer.position = position;
            pointer.direction = direction;
            pointer.player = player;
        });

    private void Start()
    {
        if (player != null) line = UI.Component<LineRenderer>(gameObject, line =>
        {
            line.material.shader = DollAssets.Shader;
            line.startColor = line.endColor = color;
            line.widthMultiplier = 0f;
        });

        UI.WorldCanvas("First Circle", transform, new(), action: canvas =>
        {
            circle1 = UI.Image("Circle", canvas, 0f, 0f, 128f, 128f, color, true, true).rectTransform;
            circle1.localScale = Vector3.zero;
        });

        UI.WorldCanvas("Second Circle", transform, new(0f, 0f, .2f), action: canvas =>
        {
            circle2 = UI.Image("Circle", canvas, 0f, 0f, 96f, 96f, color, true, true).rectTransform;
            circle2.localScale = Vector3.zero;
        });

        UI.WorldCanvas("Diamond", transform, new(0f, 0f, 1.4f), action: canvas =>
        {
            pointer = UI.DiamondImage("Diamond", canvas, 100f, 100f, 1f, .4f, .4f, .4f, color).rectTransform;
            pointer.localScale = Vector3.zero;
        });

        transform.position = position + direction.normalized * .2f;
        transform.rotation = Quaternion.LookRotation(direction);

        AudioSource.PlayClipAtPoint(HudMessageReceiver.Instance.GetComponent<AudioSource>()?.clip, position);
    }

    private void Update()
    {
        if ((Lifetime += Time.deltaTime) > 5f) Destroy(gameObject);

        float scale = Lifetime < .5f ? Lifetime * 2f : Lifetime > 4.5f ? (5f - Lifetime) * 2f : 1f;
        float time = Time.time * 3f;
        float scale1 = scale * 1.2f + Mathf.Sin(time) * .2f, scale2 = scale * 1.2f + Mathf.Sin(time + 1f) * .2f;

        circle1.localScale = new(scale1, scale1, 0f);
        circle2.localScale = new(scale2, scale2, 0f);
        pointer.localScale = new(scale, scale, 0f);

        pointer.localPosition = new(0f, 0f, Mathf.Sin(time) * 10f);
        pointer.localEulerAngles = new(time * 10f, 270f, 0f);
        if (line != null) line.widthMultiplier = scale / 10f;
    }

    private void LateUpdate()
    {
        if (player != null && line != null)
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, player.position);
        }
    }
}
