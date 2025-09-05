namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Content;
using Jaket.Net.Types;
using Jaket.Sprays;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Element that draws a spray in the world, disappears after a few seconds. </summary>
public class Spray : MonoBehaviour
{
    /// <summary> Position of the spray in the world space. </summary>
    private Vector3 position, direction;
    /// <summary> Matches the color of the respective team. </summary>
    private Color color;

    /// <summary> Player who created the spray. </summary>
    private RemotePlayer owner;
    /// <summary> How many seconds has the spray existed. </summary>
    public float Lifetime;

    /// <summary> Spawns a spray at the given position. </summary>
    public static Spray Spawn(Vector3 position, Vector3 direction, Team team, RemotePlayer owner = null) =>
        Component<Spray>(Create("Spray"), s =>
        {
            s.position = position;
            s.direction = -direction;

            s.color = team.Color() with { a = .5f };
            s.owner = owner;
        });

    private void Start()
    {
        Builder.WorldCanvas(transform, default, c =>
        {
            var spray = SprayManager.Find(owner?.Id ?? AccId)?.Sprite ?? Tex.Mark;
            var title = owner?.Header.Name ?? Name(AccId);
            var width = 141f * title.Length;

            Builder.Image(Builder.Rect("Image", c, new(1960f, 1960f)),             spray,      white, ImageType.Filled).preserveAspect = true;
            Builder.Text (Builder.Rect("Label", c, new(480f, -880f, width, 240f)), title, 240, color, TextAnchor.MiddleCenter).transform.localEulerAngles = new(0f, 0f, 6f);
        }).sortingOrder = -1;

        GetComponentsInChildren<Graphic>().Each(g => Component<Outline>(g.gameObject, o =>
        {
            o.effectColor = color;
            o.effectDistance = Vector2.one * 12f;
        }));

        transform.position = position - direction.normalized * .01f;
        transform.rotation = Quaternion.LookRotation(direction);

        AudioSource.PlayClipAtPoint(SprayManager.Puh, position);
        SpawnDust(4, 1.2f);
    }

    private void Update()
    {
        static float InCubic(float t) => t * t * t;
        static float InOutCubic(float t) => t < .5f ? (InCubic(t * 2f) / 2f) : (1f - InCubic((1f - t) * 2f) / 2f);

        if (Lifetime > 58f)
        {
            var t = InOutCubic((Lifetime - 58f) / 2f);
            transform.localScale = Vector3.one * (1f - t) * .002f;
        }

        if ((Lifetime += Time.deltaTime) > 60f)
        {
            SpawnDust(1, .3f);
            Dest(gameObject);
        }
    }

    private void SpawnDust(int amount, float scale)
    {
        var prefab = AssetHelper.LoadPrefab("Assets/Particles/ImpactParticle.prefab");
        for (var i = 0; i < amount; i++)
        {
            var dust = Inst(prefab, transform.position);
            dust.transform.localScale = Vector3.one * scale;
            dust.GetComponent<AudioSource>().Stop();
        }
    }
}
