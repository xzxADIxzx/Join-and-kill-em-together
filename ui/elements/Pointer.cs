namespace Jaket.UI.Elements;

using UnityEngine;

using Jaket.Content;
using Jaket.UI;

/// <summary> Player-created pointer, disappears in a few seconds after appearing. </summary>
public class Pointer : MonoBehaviour
{
    /// <summary> Pointer color. Usually matches the color of some team. </summary>
    private Color color;
    /// <summary> Pointer position in space. </summary>
    private Vector3 position, direction;

    /// <summary> Components from which the pointer is made. </summary>
    private RectTransform circle1, circle2, pointer;

    /// <summary> Spawns a pointer at the given position. </summary>
    public static GameObject Spawn(Team team, Vector3 position, Vector3 direction) =>
        UI.Component<Pointer>(UI.Object("Pointer"), pointer =>
        {
            pointer.color = team.Data().Color();
            pointer.color.a = .85f;

            pointer.position = position;
            pointer.direction = direction;
        }).gameObject;

    private void Start()
    {
        UI.WorldCanvas("First Circle", transform, new(), action: canvas =>
        {
            circle1 = UI.Image("Circle", canvas, 0f, 0f, 128f, 128f, color, true, true).rectTransform;
        });

        UI.WorldCanvas("Second Circle", transform, new(0f, 0f, .2f), action: canvas =>
        {
            circle2 = UI.Image("Circle", canvas, 0f, 0f, 96f, 96f, color, true, true).rectTransform;
        });

        UI.WorldCanvas("Diamond", transform, new(0f, 0f, 1.4f), action: canvas =>
        {
            pointer = UI.DiamondImage("Diamond", canvas, 100f, 100f, 1f, .4f, .4f, .4f, color).rectTransform;
        });

        transform.position = position + direction.normalized * .2f;
        transform.rotation = Quaternion.LookRotation(direction);

        AudioSource.PlayClipAtPoint(HudMessageReceiver.Instance.GetComponent<AudioSource>()?.clip, position);
        Invoke("Destroy", 5f);
    }

    private void Update()
    {
        float time = Time.time * 3f;
        float scale1 = 1.2f + Mathf.Sin(time) * .2f, scale2 = 1.2f + Mathf.Sin(time + 1f) * .2f;

        circle1.localScale = new(scale1, scale1, 0f);
        circle2.localScale = new(scale2, scale2, 0f);
        pointer.localPosition = new(0f, 0f, Mathf.Sin(time) * 10f);
        pointer.localEulerAngles = new(time * 8f, 270f, 0f);
    }

    /// <summary> Destroys the pointer. Needed only because Invoke cannot call a delegate. </summary>
    public void Destroy() => Destroy(gameObject);
}
