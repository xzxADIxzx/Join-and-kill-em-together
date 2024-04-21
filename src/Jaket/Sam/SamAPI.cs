namespace Jaket.Sam;

using System;
using UnityEngine;

using Jaket.Assets;

/// <summary> Auxiliary class of the SAM TTS Engine. Needed to simplify working with the engine. </summary>
public class SamAPI
{
    /// <summary> Sam instance used by the API. </summary>
    public static Sam Sam = new();

    /// <summary> Processes and returns an array of floats containing the sound. </summary>
    public static float[] Say(string text)
    {
        text += "["; // add the end of line character so as not to process unnecessary information

        Sam.Text2Phonemes(text, out int[] output);
        Sam.SetInput(output);

        return Sam.GetBuffer().GetFloats();
    }

    /// <summary> Wraps Sam's voice into an audio clip. </summary>
    public static AudioClip Clip(string text)
    {
        float[] data = Say(text);

        AudioClip clip = AudioClip.Create("Sam", data.Length, 1, 22050, false);
        clip.SetData(data, 0); // Sam uses a very rare frequency of 22050 hertz

        return clip;
    }

    /// <summary> Tries to speak the text via the given audio source. </summary>
    public static void TryPlay(string text, AudioSource source)
    {
        try
        {
            source.clip = Clip(Bundle.CutColors(text));
            source.Play();
        }
        catch (Exception) { } // in fact, anything can happen, that's why try catch is needed
    }
}
