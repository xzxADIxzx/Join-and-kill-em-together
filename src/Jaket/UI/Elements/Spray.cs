namespace Jaket.UI.Elements;

using UnityEngine;

using Jaket.UI;

/// <summary> Represents a player created spray that contains a image. </summary>
public class Spray : MonoBehaviour
{
    /// <summary> Spray's position in space. </summary>
    private Vector3 position, direction;
    
    public Texture2D Texture;
    private Transform Canvas;

    /// <summary> How long the spray will last in seconds. </summary>
    public float Lifetime = 20f;
    private float ProcessTime = 0f;

    /// <summary> Creates a new spray at the given position with the given direction. </summary> 
    public static Spray Spawn(Vector3 position, Vector3 direction) =>
        UI.Component<Spray>(UI.Object("Spray"), spray =>
        {
            spray.position = position;
            spray.direction = direction;
        });

    public void AssignImage(Texture2D texture)
    {
        Texture = texture;
 
        // creates the image in the world
        Canvas = UI.WorldCanvas("Spray image", transform, new(), action: canvas => UI.ImageFromTexture("Image", canvas, 0f, 0f, Texture, 128f, 128f));
        // ADI's implementation is set sorting order to 1000, so we need to set it to -1, because it causes rendering issues
        Canvas.GetComponent<Canvas>().sortingOrder = -1; 
    }

    /// <summary> Spawns a white dust particles. </summary>
    public void SpawnDust(int amount = 3, float scale = 1f)
    {
        var particlePrefab = AssetHelper.LoadPrefab("Assets/Particles/ImpactParticle.prefab");
        for (var i = 0; i < amount; i++) // make it look more cloudy
        {
            var particle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            particle.transform.localScale = Vector3.one * scale;
            // don't play the sound, because we need only particle
            particle.GetComponent<AudioSource>().Stop();
        }
    }

    #region cubic interpolation

    public static float InCubic(float t) => t * t * t;
    public static float InOutCubic(float t)
    {
        if (t < 0.5) return InCubic(t * 2) / 2;
        return 1 - InCubic((1 - t) * 2) / 2;
    }

    #endregion

    private void Start()
    {
        transform.position = position + direction.normalized * .01f; // adding some offset to prevent z-fighting
        transform.rotation = Quaternion.LookRotation(direction);
        transform.rotation *= Quaternion.Euler(0, 180, 0); // rotates the spray so that it always faces the player

        SpawnDust();
    }

    private void Update()
    {
        // destroy the spray if it's too old
        if ((ProcessTime += Time.deltaTime) > Lifetime) 
        {
            SpawnDust(1, .3f);
            Destroy(gameObject);
            return;
        }

        if (Canvas == null) return;

        // Shrink the spray as it gets older
        var shrinkTime = Lifetime * 0.2f;
        var remaining = Lifetime - shrinkTime;
        if (ProcessTime >= remaining)
        {
            var t = (ProcessTime - remaining) / shrinkTime;
            t = InOutCubic(t); // cubic interpolation looks better
            var scale = (1f - t) * .02f; // initial scale is 0.02 
            Canvas.localScale = Vector3.one * scale;
        }
    }
}