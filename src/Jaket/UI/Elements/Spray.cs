namespace Jaket.UI.Elements;

using UnityEngine;
using Jaket.UI;
/// <summary> Represents a player created spray that contains a image. </summary>
public class Spray : MonoBehaviour
{
    /// <summary> Spray's position in space. </summary>
    private Vector3 Position, Direction;

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
        var canvas = UI.WorldCanvas("Spray image", transform, new(), action: canvas => UI.ImageFromTexture2D("Image", canvas, 0f, 0f, SprayManager.CurrentSprayTexture, 128f, 128f));
        canvas.GetComponent<Canvas>().sortingOrder = -1; // ADI's implementation is set sorting order to 1000, so we need to set it to -1, because it causes rendering issues
        transform.position = Position + Direction.normalized * .01f; // adding some offset to prevent z-fighting

        transform.rotation = Quaternion.LookRotation(Direction);
        // rotates the spray so that it always faces the player
        transform.rotation *= Quaternion.Euler(0, 180, 0);

        var particlePrefab = AssetHelper.LoadPrefab("Assets/Particles/ImpactParticle.prefab");
        for (var i = 0; i < 3; i++) // To make it look more cloudy
        {
            var particle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            // don't play the sound, because we need only particle
            particle.GetComponent<AudioSource>().Stop();
        }
    }
}