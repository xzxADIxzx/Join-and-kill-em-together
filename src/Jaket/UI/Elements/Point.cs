namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;
using Jaket.UI.Lib;

/// <summary> Element that draws a point in the world, disappears after a few seconds. </summary>
public class Point : MonoBehaviour
{
    /// <summary> Position of the point in the world space. </summary>
    private Vector3 position, direction;
    /// <summary> Matches the color of the respective team. </summary>
    private Color color;

    /// <summary> Player who created the point. </summary>
    private RemotePlayer owner;
    /// <summary> Line going from the point to the player. </summary>
    private LineRenderer line;

    /// <summary> Components that the point is made of. </summary>
    private RectTransform circle1, circle2, diamond;
    /// <summary> How many seconds has the pointer existed. </summary>
    public float Lifetime;

    /// <summary> Spawns a point at the given position. </summary>
    public static Point Spawn(Vector3 position, Vector3 direction, Team team, RemotePlayer owner = null) =>
        Component<Point>(Create("Point"), p =>
        {
            p.position = position;
            p.direction = -direction;

            p.color = team.Color() with { a = .5f };
            p.owner = owner;
        });

    private void Start()
    {
        if (owner != null) line = Component<LineRenderer>(gameObject, l =>
        {
            l.startColor = l.endColor = color;
            l.widthMultiplier = 0f;
            l.material.shader = ModAssets.Additv;
        });

        Builder.WorldCanvas(Create("Cirlce", transform).transform, Vector3.back * .2f, c =>
        {
            circle1 = Builder.Image(Builder.Rect("Image", c, new(1280f, 1280f)), Tex.Circle, color, ImageType.Simple).rectTransform;
            circle1.localScale = Vector3.zero;
        });

        Builder.WorldCanvas(Create("Cirlce", transform).transform, Vector3.back * .4f, c =>
        {
            circle2 = Builder.Image(Builder.Rect("Image", c, new(960f, 960f)), Tex.Circle, color, ImageType.Simple).rectTransform;
            circle2.localScale = Vector3.zero;
        });

        Builder.WorldCanvas(Create("Diamond", transform).transform, Vector3.back * 1.6f, c =>
        {
            diamond = Builder.Diamond(Builder.Rect("Image", c, new(1000f, 1000f)), color, .4f, .4f, 1f, .4f, true).rectTransform;
            diamond.localScale = Vector3.zero;
        });

        GetComponentsInChildren<Graphic>().Each(g => (g.material = Instantiate(g.material)).shader = ModAssets.Additv);

        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction);

        AudioSource.PlayClipAtPoint(HudMessageReceiver.Instance.GetComponent<AudioSource>()?.clip, position);
    }

    private void Update()
    {
        if ((Lifetime += Time.deltaTime) > 6f) Dest(gameObject);

        float scale = Lifetime < .5f ? Lifetime * 2f : Lifetime > 5.5f ? (6f - Lifetime) * 2f : 1f;
        float time = Time.time * 3f;

        circle1.localScale = Vector3.one * (scale * 1.2f + Mathf.Sin(time + 1f) * .2f);
        circle2.localScale = Vector3.one * (scale * 1.2f + Mathf.Sin(time + 2f) * .2f);
        diamond.localScale = Vector3.one * (scale);

        diamond.localPosition = new(0f, 0f, Mathf.Sin(time + 2.5f) * 128f);
        diamond.localEulerAngles = new(time * 20f, 270f, 0f);

        if (line) line.widthMultiplier = scale / 10f;
    }

    private void LateUpdate()
    {
        if (line)
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, owner.Position - Vector3.up * 2.5f);
        }
    }
}
