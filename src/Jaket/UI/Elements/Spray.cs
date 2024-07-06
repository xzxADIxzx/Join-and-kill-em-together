namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.Sprays;
using Jaket.UI.Dialogs;

/// <summary> Player-created spray containing an image, disappears in a few seconds after appearing. </summary>
public class Spray : MonoBehaviour
{
    /// <summary> Owner of the spray. </summary>
    private uint owner;
    /// <summary> Spray position in space. </summary>
    private Vector3 position, direction;

    /// <summary> Image component of the spray. </summary>
    private Image image;
    /// <summary> How many seconds has the spray existed. </summary>
    public float Lifetime;

    /// <summary> Spawns a spray at the given position. </summary>
    public static Spray Spawn(uint owner, Vector3 position, Vector3 direction) =>
        UIB.Component<Spray>(Tools.Create("Spray"), spray =>
        {
            spray.owner = owner;
            spray.position = position;
            spray.direction = direction;
        });

    private void Start()
    {
        UIB.WorldCanvas("Canvas", transform, Vector3.zero, build: canvas => image = UIB.Image("Image", canvas, new(0f, 0f, 256f, 256f), type: Image.Type.Filled));
        UpdateSprite();

        image.preserveAspect = true;
        transform.position = position + direction.normalized * .01f;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180, 0);

        AudioSource.PlayClipAtPoint(SprayManager.puh, position);
        SpawnDust();
    }

    private void Update()
    {
        if (Lifetime > 58f)
        {
            var t = InOutCubic((Lifetime - 58f) / 2f); // cubic interpolation looks better 
            image.transform.localScale = Vector3.one * (1f - t);
        }

        if ((Lifetime += Time.deltaTime) > 60f)
        {
            SpawnDust(1, .3f);
            Destroy(gameObject);
            return;
        }
    }

    /// <summary> Updates the image's sprite. </summary>
    public void UpdateSprite() => image.sprite =
        SprayManager.Cache.TryGetValue(owner, out var spray) && SpraySettings.Enabled && !Administration.BannedSprays.Contains(owner)
        ? spray.Sprite : UIB.Checkmark;

    /// <summary> Spawns white dust particles. </summary>
    public void SpawnDust(int amount = 3, float scale = 1f)
    {
        var prefab = AssetHelper.LoadPrefab("Assets/Particles/ImpactParticle.prefab");
        for (var i = 0; i < amount; i++) // make it look more cloudy
        {
            var particle = Instantiate(prefab, transform.position, Quaternion.identity);
            particle.transform.localScale = Vector3.one * scale;
            particle.GetComponent<AudioSource>().Stop(); // don't play the sound, we need only the particles
        }
    }

    public static float InCubic(float t) => t * t * t;
    public static float InOutCubic(float t) => t < 0.5 ? (InCubic(t * 2) / 2) : (1 - InCubic((1 - t) * 2) / 2);
}
