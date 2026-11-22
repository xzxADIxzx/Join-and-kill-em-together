namespace Jaket.World;

using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.Input;

/// <summary> Class responsible for managing on-teleport camera distortions. </summary>
public static class Teleporter
{
    /// <summary> Triggers a temporary camera distortion effect and a sound cue that gradually fade out over time. </summary>
    public static IEnumerator Flash(float distortion)
    {
        GameAssets.Sound("UI/CSFX-2_Mechanics_45.wav", c =>
        {
            AudioSource.PlayClipAtPoint(c, cctr.position);
        });

        pp.DeathEffect(true);
        pp.WickedEffect(true);

        while (distortion > .01f)
        {
            Shader.SetGlobalFloat("_Deathness",           distortion);
            Shader.SetGlobalFloat("_RandomNoiseStrength", distortion);

            distortion = Mathf.Lerp(distortion, 0f, Time.deltaTime * 12f);
            yield return null;
        }

        pp.DeathEffect(false);
        pp.WickedEffect(false);
    }

    /// <summary> Triggers a temporary camera distortion effect and a sound cue that gradually fade out over time. </summary>
    public static void Flash() => Plugin.Instance.StartCoroutine(Flash(4f));

    /// <summary> Teleports the player to the given position. </summary>
    public static void Tp(Vector3 position, bool flash = true)
    {
        Movement.UpdateState();
        nmtr.position = position;
        nm.rb.velocity = default;

        if (flash) Flash();

        PlayerActivatorRelay.Instance?.Activate();
        if (GameStateManager.Instance.IsStateActive("pit-falling"))
            GameStateManager.Instance.PopState("pit-falling");

        ObjFind("Hellmap").Disable();
    }

    /// <summary> Whether the player is inside the given box. </summary>
    public static bool Inside(params float[] p) => nmtr.position.x > p[0] && nmtr.position.y > p[1] && nmtr.position.z > p[2]
                                                && nmtr.position.x < p[3] && nmtr.position.y < p[4] && nmtr.position.z < p[5];
}
