namespace Jaket.UI.Elements;

using UnityEngine;

using Jaket.UI;

/// <summary> Represents a player created spray that contains a image. </summary>
public class Spray : MonoBehaviour
{
    /// <summary> Spray's position in space. </summary>
    private Vector3 Position, Direction;

    private Transform Canvas;

    /// <summary> How long the spray will last in seconds. </summary>
    public float Lifetime = 10f;
    private float ProcessTime = 0f;

    /// <summary> Creates a new spray at the given position with the given direction. </summary> 
    public static Spray Spawn(Vector3 position, Vector3 direction) =>
        UI.Component<Spray>(UI.Object("Spray"), spray =>
        {
            spray.Position = position;
            spray.Direction = direction;
        });

    public void Start()
    {
        if (SprayManager.CurrentSprayTexture == null)
        {
            Log.Warning("Spray you want to create is invalid!");
            return;
        }

        // creates the image in the world
        Canvas = UI.WorldCanvas("Spray image", transform, new(), action: canvas => UI.ImageFromTexture("Image", canvas, 0f, 0f, SprayManager.CurrentSprayTexture, 128f, 128f));
        Canvas.GetComponent<Canvas>().sortingOrder = -1; // ADI's implementation is set sorting order to 1000, so we need to set it to -1, because it causes rendering issues
        
        transform.position = Position + Direction.normalized * .01f; // adding some offset to prevent z-fighting
        transform.rotation = Quaternion.LookRotation(Direction);
        transform.rotation *= Quaternion.Euler(0, 180, 0); // rotates the spray so that it always faces the player

        SpawnDust();
    }

    /// <summary> Spawns a white dust particles. </summary>
    public void SpawnDust(int amount = 3, float scale = 1f)
    {
        var particlePrefab = AssetHelper.LoadPrefab("Assets/Particles/ImpactParticle.prefab");
        for (var i = 0; i < amount; i++) // To make it look more cloudy
        {
            var particle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            particle.transform.localScale = new Vector3(scale, scale, scale);
            // don't play the sound, because we need only particle
            particle.GetComponent<AudioSource>().Stop();
        }
    }

    public static float InCubic(float t) => t * t * t;
    public static float InOutCubic(float t)
    {
        if (t < 0.5) return InCubic(t * 2) / 2;
        return 1 - InCubic((1 - t) * 2) / 2;
    }

    public void Update()
    {
        if ((ProcessTime += Time.deltaTime) > Lifetime) 
        {
            SpawnDust(1, .3f);
            Destroy(gameObject);
            return;
        }

        // Shrink the spray as it gets older
        var shrinkTime = Lifetime * 0.2f;
        var remaining = Lifetime - shrinkTime;
        if (ProcessTime >= remaining)
        {
            var t = (ProcessTime - remaining) / shrinkTime;
            t = InOutCubic(t); // cubic interpolation looks better
            var scale = (1f - t) * .02f; // initial scale is 0.02
            Canvas.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}