namespace Jaket.Sam;

using System.Text;
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
    public Buffer Buffer;

    /// <summary> Object for working with decompiled code that cannot be understood. </summary>
    public Legacy Legacy;

    public Sam(int speed = 64, int pitch = 64, int mouth = 128, int throat = 128)
    {   // default settings with changed speed
        Speed = speed;
        Pitch = pitch;
        Mouth = mouth;
        Throat = throat;
        Legacy = new(this);
    }

    /// <summary> Translates the given text from Cyrillic to Latin. </summary>
    public string Cyrillic2Latin(string text)
    {
        StringBuilder builder = new();

        char prev = ' ';
        foreach (char current in text)
        {
            // example: Ездить -> Yezdit'
            if (current == 'Е' && (prev == ' ' || prev == 'Ъ' || prev == 'Ь'))
                builder.Append("YE");

            // look for a symbol in the list of transformations
            else if (Constants.ISO9.ContainsKey(current))
                builder.Append(Constants.ISO9[current]);

            // if it is not there, then the symbol is not Cyrillic
            else
                builder.Append(current);

            // save the previous symbol for the correct pronunciation of the letter E
            prev = current;
        }

        return builder.ToString();
    }

    /// <summary> Converts the given text into phonemes and returns them as an array of integers. </summary>
    public void Text2Phonemes(string text, out int[] output)
    {
        var bytes = Encoding.UTF8.GetBytes(Cyrillic2Latin(text.ToUpper()));

        output = new int[256];
        for (int i = 0; i < bytes.Length; i++) output[i] = bytes[i];

        Legacy.Text2Phonemes(ref output);
    }

    /// <summary> Changes the text that Sam will speak to the given one. </summary>
    public void SetInput(int[] input)
    {
        // copy the number of input data limited to 254 elements
        int length = Mathf.Min(input.Length, 254);
        for (int i = 0; i < length; i++) this.input[i] = input[i];

        // add end marks to the end of the input data and, just in case, to the end of the array
        this.input[length] = 255;
        this.input[255] = 255;
    }

    /// <summary> Inserts a new phoneme into the data array. </summary>
    public void Insert(int pos, int phoneme, int length = 0, int stress = -1)
    {
        // move the phonemes up, leaving only the end mark [255]
        for (int i = 253; i >= pos; i--)
        {
            phonemeIndex[i + 1] = phonemeIndex[i];
            phonemeLength[i + 1] = phonemeLength[i];
            phonemeStress[i + 1] = phonemeStress[i];
        }

        // insert new data
        phonemeIndex[pos] = phoneme;
        phonemeLength[pos] = length;
        phonemeStress[pos] = stress == -1 ? phonemeStress[pos - 1] : stress;
    }

    /// <summary> Returns a buffer with rendered audio data. </summary>
    public Buffer GetBuffer()
    {
        // clear all data arrays, except phoneme index, because it will be overwritten
        for (int i = 0; i < 256; i++) phonemeStress[i] = phonemeLength[i] = 0;

        // parsing phonemes from the given text
        if (!ParsePhonemes()) return null;

        RewritePhonemes();
        SetPhonemeLength();
        InsertPauses();
        PrepareOutput();

        return Buffer;
    }

    #region processing

    /// <summary>
    /// The input array contains a string of phonemes and stress markers along the lines of:
    ///
    ///     JAEKEHT IHZ AH GUH5D MAAD. [0x9B]
    ///
    /// Some phonemes are 2 bytes long, such as "AE" and "EH".
    /// Others are 1 byte long, such as "T" and "Z".
    /// There are also stress markers, such as "5" and ".".
    /// Byte 0x9B marks the end of the string.
    ///
    /// The characters of the phonemes are stored in the PhonemeNameTable.
    /// The stress characters are arranged in low to high stress order in StressCharTable.
    ///
    /// For parsing, the following instructions are repeated until the end marker [0x9B] is reached:
    ///
    ///     1. Looking for a full match of phonemes without the '*' character.
    ///     2. If a match is not found, the search is repeated, but only for the first character.
    ///     3. In case of failure, search in the stress table.
    ///     4. If this fails, then the input data is invalid, return false.
    ///
    /// Upon success:
    ///
    ///     1. PhonemeIndex will contain the index of the phonemes and at least one end mark.
    ///     2. PhonemeStress will contain the stress value for each phoneme 
    /// </summary>
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

        // reached the end of the string without finding the end mark
        phonemeIndex[255] = 255;
        return true;
    }

    /// <summary>
    /// Rewrites the phonemes using the following rules:
    ///
    ///     CH -> CH CH'
    ///     J -> J J'
    ///     [ALVEOLAR] UW -> [ALVEOLAR] UX
    ///     D R -> J R
    ///     T R -> CH R
    ///     [VOWEL] R -> [VOWEL] RX
    ///     [DIPHTHONG     ENDING WITH WX] -> [DIPHTHONG] WX
    ///     [DIPHTHONG NOT ENDING WITH WX] -> [DIPHTHONG] YX
    ///     UL -> AX L
    ///     UM -> AX M
    ///     UN -> AX N
    ///     [STRESSED VOWEL] [SILENCE] [STRESSED VOWEL] -> [STRESSED VOWEL] [SILENCE] Q [VOWEL]
    ///     S P -> S B    S KX -> S GX
    ///     S T -> S D    S UM -> S **
    ///     S K -> S G    S UN -> S **
    /// </summary>
    public void RewritePhonemes()
    {
        // handles 3 rules from the list of phoneme transformations
        void HandleUW_CH_J(int phoneme, int pos)
        {
            switch (phoneme)
            {
                // CH -> CH CH'
                case 42: Insert(pos + 1, 43 /* CH + 1 */); break;

                // J -> J J'
                case 44: Insert(pos + 1, 45 /* J* + 1 */); break;

                // [ALVEOLAR] UW -> [ALVEOLAR] UX
                case 53:
                    if (Constants.HasFlag(phonemeIndex[pos - 1], 0x0400)) phonemeIndex[pos] = 16;
                    break;
            }
        };

        // handles 3 rules from the list of phoneme transformations
        void HandleTR_DR_R(int phoneme, int pos)
        {
            switch (phoneme)
            {
                // D R -> J R
                case 57: phonemeIndex[pos - 1] = 44; break;

                // T R -> CH R
                case 69: phonemeIndex[pos - 1] = 42; break;

                // [VOWEL] R -> [VOWEL] RX
                default:
                    if (Constants.HasFlag(phoneme, 0x0080)) phonemeIndex[pos] = 18;
                    break;
            }
        };

        // replaces phoneme with AX + suffix
        void Change2AX(int pos, int suffix)
        {
            phonemeIndex[pos] = 13; // AX
            Insert(pos + 1, suffix);
        };

        int phoneme, pos = 0; // go through all the phonemes until reach the end marker
        while ((phoneme = phonemeIndex[++pos]) != 255)
        {
            // skip spaces because there's nothing to do with them
            if (phoneme == 0) continue;

            // [DIPHTHONG     ENDING WITH WX] -> [DIPHTHONG] WX
            // [DIPHTHONG NOT ENDING WITH WX] -> [DIPHTHONG] YX
            // UW, CH and J
            if (Constants.HasFlag(phoneme, 0x10))
            {
                Insert(pos + 1, Constants.HasFlag(phoneme, 0x20) ? 21 : 20);
                HandleUW_CH_J(phoneme, pos);
                continue;
            }

            // UL -> AX L
            if (phoneme == 78)
            {
                Change2AX(pos, 24);
                continue;
            }

            // UM -> AX M
            if (phoneme == 79)
            {
                Change2AX(pos, 27);
                continue;
            }

            // UN -> AX N
            if (phoneme == 80)
            {
                Change2AX(pos, 28);
                continue;
            }

            // [STRESSED VOWEL] [SILENCE] [STRESSED VOWEL] -> [STRESSED VOWEL] [SILENCE] Q [VOWEL]
            if (Constants.HasFlag(phoneme, 0x80) && phonemeStress[pos] != 0 && pos <= 253)
            {
                if (phonemeIndex[pos + 1] == 0) // [SILENCE]
                {
                    phoneme = phonemeIndex[pos + 2];
                    if (phoneme != 0 && Constants.HasFlag(phoneme, 0x80) && phonemeStress[pos + 2] != 0) Insert(pos + 2, 31, stress: 0);
                }
                continue;
            }

            // TR, DR and R
            if (phonemeIndex[pos - 1] == 23)
            {
                HandleTR_DR_R(phonemeIndex[pos - 1], pos);
                continue;
            }

            // S P -> S B    S KX -> S GX
            // S T -> S D    S UM -> S **
            // S K -> S G    S UN -> S **
            if (Constants.HasFlag(phoneme, 0x01))
            {
                if (phonemeIndex[pos - 1] == 32) // S
                    phonemeIndex[pos] = phoneme - 12;
            }
            else HandleUW_CH_J(phoneme, pos);
        }
    }

    /// <summary> Sets the length of phonemes depending on stress. </summary>
    public void SetPhonemeLength()
    {
        int phoneme, pos = 0; // go through all the phonemes until reach the end marker
        while ((phoneme = phonemeIndex[++pos]) != 255)
        {
            int stress = phonemeStress[pos];
            if (stress == 0 || (stress & 128) != 0) // use low or high bits depending on stress
                phonemeLength[pos] = Constants.PhonemeLengthTable[phoneme] & 0xFF;
            else
                phonemeLength[pos] = Constants.PhonemeLengthTable[phoneme] >> 8;
        }
    }

    /// <summary> It's hard to say for sure, but most likely this method inserts pauses for punctuation. </summary>
    public void InsertPauses()
    {
        int phoneme, pos = 0; // go through all the phonemes until reach the end marker
        while ((phoneme = phonemeIndex[++pos]) != 255)
        {
            // if the phoneme is already a pause, then we skip it to avoid OutOfBoundsException
            if (phoneme == 254) continue;

            // if the phoneme is a punctuation mark, insert a short pause
            if (Constants.HasFlag(phoneme, 0x0100))
            {
                Insert(++pos, 254);
                continue;
            }
        }
    }

    /// <summary> Prepares data for rendering via legacy code. </summary>
    public void PrepareOutput()
    {
        Buffer = new(); // create an output buffer for writing data through legacy code

        int phoneme, pos = 0, outputPos = 0; // go through all the phonemes until reach the end marker
        while ((phoneme = phonemeIndex[++pos]) != 255)
        {
            // skip spaces because there's nothing to do with them
            if (phoneme == 0) continue;

            // if the phoneme is a pause, then start rendering
            if (phoneme == 254)
            {
                Legacy.IndexOutput[outputPos] = 255;
                Legacy.Render();

                outputPos = 0;
                continue;
            }

            Legacy.IndexOutput[outputPos] = phoneme;
            Legacy.LengthOutput[outputPos] = phonemeLength[pos];
            Legacy.StressOutput[outputPos] = phonemeStress[pos];
            outputPos++;
        }

        Legacy.IndexOutput[outputPos] = 255;
        Legacy.Render();
    }

    #endregion
}
