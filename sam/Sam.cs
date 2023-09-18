namespace Jaket.Sam;

using UnityEngine;

/// <summary> Main class of the SAM TTS Engine. Responsible for storing and converting text into phonemes. </summary>
public class Sam
{
    /// <summary> Text phonemes recorded in an int array. </summary>
    private int[] input = new int[256];

    /// <summary> Phonemes parameters affecting their pronunciation. </summary>
    private int[] phonemeStress = new int[256], phonemeLength = new int[256], phonemeIndex = new int[256];

    /// <summary> Sam's voice settings. </summary>
    public int Speed, Pitch, Mouth, Throat;

    /// <summary> Buffer containing output data. </summary>
    public Buffer22222 Buffer;

    public Sam(int speed = 64, int pitch = 64, int mouth = 128, int throat = 128)
    {   // default settings with changed speed
        this.Speed = speed;
        this.Pitch = pitch;
        this.Mouth = mouth;
        this.Throat = throat;
    }

    /// <summary> Changes the text that Sam will speak to the given one. </summary>
    public void SetInput(int[] input)
    {
        // copy the number of input data limited to 256 elements
        int length = Mathf.Min(input.Length, this.input.Length);
        for (int i = 0; i < length; i++) this.input[i] = input[i];

        // add end marks to the end of the input data and, just in case, to the end of the array
        this.input[length] = 255;
        this.input[255] = 255;
    }

    /// <summary> Returns a buffer with rendered audio data. </summary>
    public Buffer22222 GetBuffer()
    {
        // clear all data arrays, except phoneme index, because it will be overwritten
        for (int i = 0; i < 256; i++) phonemeStress[i] = phonemeLength[i] = 0;

        // parsing phonemes from the given text
        if (!ParsePhonemes()) return null;

        SetPhonemeLength();

        return Buffer;
    }

    #region processing

    public bool ParsePhonemes()
    {
        // position in the output array
        int outputPos = 0;

        // go through the input data
        for (int pos = 0; pos < input.Length; pos++)
        {
            var sign1 = (char)input[pos];
            var sign2 = (char)(pos == input.Length - 1 ? ' ' : input[pos + 1]);

            // reached the end mark, success
            if (sign1 == 155)
            {
                phonemeIndex[outputPos++] = 255;
                return true;
            }

            // point 1, full match 
            int match = Constants.FullMatch(sign1, sign2);
            if (match != -1)
            {
                pos++; // skip the second character
                phonemeIndex[outputPos++] = match;
                continue;
            }

            // point 2, wild match
            match = Constants.WildMatch(sign1);
            if (match != -1)
            {
                phonemeIndex[outputPos++] = match;
                continue;
            }

            // point 3, stress mark
            match = Constants.StressCharTable.IndexOf(sign1);
            if (match != -1)
                // set the stress of the previous phoneme
                phonemeStress[outputPos - 1] = match + 1;
            else
                // unknown character, failed to parse the input
                return false;
        }

        // reached the end of the line without finding the end mark
        phonemeIndex[255] = 255;
        return true;
    }

    /// <summary> Sets the length of phonemes depending on stress. </summary>
    public void SetPhonemeLength()
    {
        int phoneme, pos = 0; // go through all the phonemes until reach the end marker
        while ((phoneme = phonemeIndex[pos]) != 255)
        {
            int stress = phonemeStress[pos];
            if (stress == 0 || (stress & 128) != 0) // use low or high bits depending on stress
                phonemeLength[pos] = Constants.PhonemeLengthTable[phoneme] & 0xFF;
            else
                phonemeLength[pos] = Constants.PhonemeLengthTable[phoneme] >> 8;

            pos++; // move on
        }
    }

    #endregion
}
