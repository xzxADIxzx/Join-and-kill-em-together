namespace Jaket.Sam;

using System;
using UnityEngine;

/// <summary>
/// This class contain all the code that even with the help of God cannot be understood.
/// Abandon hope all ye who enter here...
/// </summary>
public class Legacy
{
    /// <summary> Link to Sam instance running legacy code. </summary>
    public Sam Sam;

    public Legacy(Sam sam) => this.Sam = sam;

    /// <summary> Increments the given value by 1, but limits it to one byte. </summary>
    public static void Inc(ref int value) => value = ++value & 255;

    /// <summary> Decrements the given value by 1, but limits it to one byte. </summary>
    public static void Dec(ref int value) => value = --value & 255;

    #region phonemes

    /// <summary> Something like registers. </summary>
    private int A, X, Y;
    /// <summary> Copy of input from Sam instance. </summary>
    private int[] temp;

    public bool Text2Phonemes(ref int[] input)
    {
        void UnknownCode1(int mem59) => A = Constants.Tab1[Y = temp[X = mem59 - 1]];

        void UnknownCode2(int mem58)
        {
            X = mem58;
            Inc(ref X);
            A = Constants.Tab1[Y = temp[X]];
        }

        int GetRuleByte(int mem62, int Y) => mem62 >= 37541 ? Constants.RulesSet2[mem62 - 37541 + Y] : Constants.RulesSet1[mem62 - 32000 + Y];

        int mem56 = 255, mem57, mem58, mem59, mem60, mem61 = 255, mem62, mem64 = 0, mem65, mem66;

        temp = new int[256];
        temp[0] = 32;

        X = 1;
        Y = 0;
        do
        {
            A = input[Y] & 127;
            if (A >= 112) A &= 95;
            else if (A >= 96) A &= 79;

            temp[X] = A;
            Inc(ref X);
            Inc(ref Y);
        } while (Y != 255);

        A = 255;
        X = 255;
        temp[X] = 27;

    pos36554:
        while (true)
        {
            Inc(ref mem61);
            X = mem61;
            if (X >= temp.Length) break;
            mem64 = A = temp[X];
            if (A == '[')
            {
                Inc(ref mem56);
                X = mem56;

                A = 155;
                input[X] = 155;
                Inc(ref X);

                var copy = new int[X];
                Array.Copy(input, copy, X);
                input = copy;

                return true;
            }
            if (A != '.') break;
            Inc(ref X);
            Y = temp[X];
            A = Constants.Tab1[Y] & 1;
            if (A != 0) break;
            Inc(ref mem56);
            X = mem56;
            input[X] = '.';
        }
        A = mem64;
        Y = A;
        A = Constants.Tab1[A];
        mem57 = A;
        if ((A & 2) != 0)
        {
            mem62 = 37541;
            goto pos36700;
        }
        A = mem57;
        if (A != 0) goto pos36677;
        A = 32;
        if (X >= temp.Length) return true;
        temp[X] = ' ';
        Inc(ref mem56);
        X = mem56;
        if (X > 120) goto pos36654;
        input[X] = A;
        goto pos36554;

    pos36654:
        input[X] = 155;
        A = mem61;
        return true;

    pos36677:
        A = mem57 & 128;
        if (A == 0) return false;
        X = mem64 - 'A';
        mem62 = Constants.Tab2[X] | (Constants.Tab3[X] << 8);

    pos36700:
        Y = 0;
        do
        {
            mem62++;
            A = GetRuleByte(mem62, Y);
        } while ((A & 128) == 0);
        Inc(ref Y);
        while (true)
        {
            A = GetRuleByte(mem62, Y);
            if (A == '(') break;
            Inc(ref Y);
        }
        mem66 = Y;
        do
        {
            Inc(ref Y);
            A = GetRuleByte(mem62, Y);
        } while (A != ')');
        mem65 = Y;
        do
        {
            Inc(ref Y);
            A = GetRuleByte(mem62, Y);
            A &= 127;
        } while (A != '=');
        mem64 = Y;
        X = mem61;
        mem60 = X;
        Y = mem66;
        Inc(ref Y);
        while (true)
        {
            mem57 = temp[X];
            A = GetRuleByte(mem62, Y);
            if (A != mem57) goto pos36700;
            Inc(ref Y);
            if (Y == mem65) break;
            Inc(ref X);
            mem60 = X;
        }
        A = mem61;
        mem59 = mem61;

    pos36791:
        while (true)
        {
            Dec(ref mem66);
            Y = mem66;
            A = GetRuleByte(mem62, Y);
            mem57 = A;
            if ((A & 128) != 0) goto pos37180;
            X = A & 127;
            A = Constants.Tab1[X] & 128;
            if (A == 0) break;
            X = mem59 - 1;
            A = temp[X];
            if (A != mem57) goto pos36700;
            mem59 = X;
        }
        A = mem57;
        if (A == ' ') goto pos36895;
        if (A == '#') goto pos36910;
        if (A == '.') goto pos36920;
        if (A == '&') goto pos36935;
        if (A == '@') goto pos36967;
        if (A == '^') goto pos37004;
        if (A == '+') goto pos37019;
        if (A == ':') goto pos37040;
        return false;

    pos36895:
        UnknownCode1(mem59);
        A &= 128;
        if (A != 0) goto pos36700;
        pos36905:
        mem59 = X;
        goto pos36791;

    pos36910:
        UnknownCode1(mem59);
        A &= 64;
        if (A != 0) goto pos36905;
        goto pos36700;

    pos36920:
        UnknownCode1(mem59);
        A &= 8;
        if (A == 0) goto pos36700;
        pos36930:
        mem59 = X;
        goto pos36791;

    pos36935:
        UnknownCode1(mem59);
        A &= 16;
        if (A != 0) goto pos36930;
        A = temp[X];
        if (A != 72) goto pos36700;
        Dec(ref X);
        A = temp[X];
        if ((A == 67) || (A == 83)) goto pos36930;
        goto pos36700;

    pos36967:
        UnknownCode1(mem59);
        A &= 4;
        if (A != 0) goto pos36930;
        A = temp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem59 = X;
        goto pos36791;


    pos37004:
        UnknownCode1(mem59);
        A &= 32;
        if (A == 0) goto pos36700;
        pos37014:
        mem59 = X;
        goto pos36791;

    pos37019:
        X = mem59;
        Dec(ref X);
        A = temp[X];
        if ((A == 'E') || (A == 'I') || (A == 'Y')) goto pos37014;
        goto pos36700;

    pos37040:
        UnknownCode1(mem59);
        A &= 32;
        if (A == 0) goto pos36791;
        mem59 = X;
        goto pos37040;

    pos37077:
        X = mem58 + 1;
        A = temp[X];
        if (A != 'E') goto pos37157;
        Inc(ref X);
        Y = temp[X];
        Dec(ref X);
        A = Constants.Tab1[Y] & 128;
        if (A == 0) goto pos37108;
        Inc(ref X);
        A = temp[X];
        if (A != 'R') goto pos37113;
        pos37108:
        mem58 = X;
        goto pos37184;

    pos37113:
        if ((A == 83) || (A == 68)) goto pos37108;
        if (A != 76) goto pos37135;
        Inc(ref X);
        A = temp[X];
        if (A != 89) goto pos36700;
        goto pos37108;

    pos37135:
        if (A != 70) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if (A != 85) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if (A == 76) goto pos37108;
        goto pos36700;

    pos37157:
        if (A != 73) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if (A != 78) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if (A == 71) goto pos37108;
        goto pos36700;

    pos37180:
        A = mem60;
        mem58 = A;

    pos37184:
        Y = mem65 + 1;
        if (Y == mem64) goto pos37455;
        mem65 = Y;
        A = GetRuleByte(mem62, Y);
        mem57 = A;
        X = A;
        A = Constants.Tab1[X] & 128;
        if (A == 0) goto pos37226;
        X = mem58 + 1;
        A = temp[X];
        if (A != mem57) goto pos36700;
        mem58 = X;
        goto pos37184;

    pos37226:
        A = mem57;
        if (A == 32) goto pos37295;
        if (A == 35) goto pos37310;
        if (A == 46) goto pos37320;
        if (A == 38) goto pos37335;
        if (A == 64) goto pos37367;
        if (A == 94) goto pos37404;
        if (A == 43) goto pos37419;
        if (A == 58) goto pos37440;
        if (A == 37) goto pos37077;
        if (A == 37) goto pos37077;
        return false;

    pos37295:
        UnknownCode2(mem58);
        A &= 128;
        if (A != 0) goto pos36700;
        pos37305:
        mem58 = X;
        goto pos37184;

    pos37310:
        UnknownCode2(mem58);
        A &= 64;
        if (A != 0) goto pos37305;
        goto pos36700;

    pos37320:
        UnknownCode2(mem58);
        A &= 8;
        if (A == 0) goto pos36700;
        pos37330:
        mem58 = X;
        goto pos37184;

    pos37335:
        UnknownCode2(mem58);
        A &= 16;
        if (A != 0) goto pos37330;
        A = temp[X];
        if (A != 72) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if ((A == 67) || (A == 83)) goto pos37330;
        goto pos36700;

    pos37367:
        UnknownCode2(mem58);
        A &= 4;
        if (A != 0) goto pos37330;
        A = temp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem58 = X;
        goto pos37184;

    pos37404:
        UnknownCode2(mem58);
        A &= 32;
        if (A == 0) goto pos36700;
        pos37414:
        mem58 = X;
        goto pos37184;

    pos37419:
        X = mem58;
        Inc(ref X);
        A = temp[X];
        if ((A == 69) || (A == 73) || (A == 89)) goto pos37414;
        goto pos36700;

    pos37440:
        UnknownCode2(mem58);
        A &= 32;
        if (A == 0) goto pos37184;
        mem58 = X;
        goto pos37440;

    pos37455:
        Y = mem64;
        mem61 = mem60;

    pos37461:
        A = GetRuleByte(mem62, Y);
        mem57 = A;
        A &= 127;
        if (A != '=')
        {
            Inc(ref mem56);
            X = mem56;
            input[X] = A;
        }
        if ((mem57 & 128) == 0) goto pos37485;
        goto pos36554;

    pos37485:
        Inc(ref Y);
        goto pos37461;
    }

    #endregion
    #region renderer

    public int[] StressOutput = new int[256];
    public int[] LengthOutput = new int[256];
    public int[] IndexOutput = new int[256];

    private int[] pitches = new int[256];
    private int[] sampledConsonantFlag = new int[256];
    private int[,] frequency = new int[3, 256];
    private int[,] amplitude = new int[3, 256];

    private int mem39, mem44, mem47, mem49, mem50, mem51, mem53, mem56;

    /// <summary> Adds an inflection to intonation to indicate a question mark and etc. </summary>
    private void AddInflection(int mem48, int phase1)
    {
        int Atemp = mem49 = A = X;

        A -= 30;
        if (Atemp <= 30) A = 0;
        X = A;
        while ((A = pitches[X]) == 127) Inc(ref X);

        pos48398: pitches[X] = phase1 = (A + mem48) & 255;

    pos48406:
        Inc(ref X);
        if (X == mem49) return;
        if (pitches[X] == 255) goto pos48406;
        A = phase1;
        goto pos48398;
    }

    private void RenderSample(ref int mem66)
    {
        int tempA = 0;
        mem49 = Y;
        A = mem39 & 7;
        mem53 = Constants.Tab5[mem47 = mem56 = X = A - 1];
        A = mem39 & 248;
        if (A == 0)
        {
            A = pitches[mem49] >> 4;
            goto pos48315;
        }
        Y = A ^ 255;

    pos48274:
        mem56 = 8;
        A = Constants.SampleTable[mem47 * 256 + Y];

    pos48280:
        tempA = A;
        A <<= 1;
        if ((tempA & 128) == 0)
        {
            X = mem53;
            Sam.Buffer.Write(1, (X & 0x0f) * 16);
            if (X != 0) goto pos48296;
        }
        Sam.Buffer.Write(2, 5 * 16);

    pos48296:
        X = 0;
        Dec(ref mem56);
        if (mem56 != 0) goto pos48280;
        Inc(ref Y);
        if (Y != 0) goto pos48274;
        mem44 = 1;
        Y = mem49;
        return;

    pos48315:
        int phase1 = A ^ 255;
        Y = mem66;
        do
        {
            mem56 = 8;
            A = Constants.SampleTable[mem47 * 256 + Y];
            do
            {
                tempA = A;
                A <<= 1;
                if ((tempA & 128) != 0)
                    Sam.Buffer.Write(3, 160);
                else
                    Sam.Buffer.Write(4, 96);
                Dec(ref mem56);
            } while (mem56 != 0);
            Inc(ref Y);
            Inc(ref phase1);
        } while (phase1 != 0);
        A = 1;
        mem44 = 1;
        mem66 = Y;
        Y = mem49;
        return;
    }

    public void Render()
    {
        int Read(int type, int index) => type switch
        {
            168 => pitches[index],
            169 => frequency[0, index],
            170 => frequency[1, index],
            171 => frequency[2, index],
            172 => amplitude[0, index],
            173 => amplitude[1, index],
            174 => amplitude[2, index],
            _ => 0
        };

        void Write(int type, int index, int value)
        {
            switch (type)
            {
                case 168: pitches[Y] = value; break;
                case 169: frequency[0, Y] = value; break;
                case 170: frequency[1, Y] = value; break;
                case 171: frequency[2, Y] = value; break;
                case 172: amplitude[0, Y] = value; break;
                case 173: amplitude[1, Y] = value; break;
                case 174: amplitude[2, Y] = value; break;
            }
        }

        int phase1 = 0, phase2, phase3, speedcounter;
        int mem38, mem40, mem48, mem66 = 0;

        if (IndexOutput[0] == 255) return;

        A = X = mem44 = 0;
        do
        {
            mem56 = A = IndexOutput[Y = mem44];
            if (A == 255) break;

            if (A == 1) AddInflection(mem48 = A = 1, phase1);
            if (A == 2) AddInflection(mem48 = 255, phase1);

            phase1 = Constants.Tab4[StressOutput[Y] + 1];
            phase2 = LengthOutput[Y];
            Y = mem56;

            do
            {
                pitches[X] = Sam.Pitch + phase1;
                sampledConsonantFlag[X] = Constants.SampledConsonantFlags[Y];
                frequency[0, X] = Constants.PhonemeFrequencyTable[Y] & 0xFF;
                frequency[1, X] = (Constants.PhonemeFrequencyTable[Y] >> 8) & 0xFF;
                frequency[2, X] = (Constants.PhonemeFrequencyTable[Y] >> 16) & 0xFF;
                amplitude[0, X] = Constants.PhonemeAmplitudesTable[Y] & 0xFF;
                amplitude[1, X] = (Constants.PhonemeAmplitudesTable[Y] >> 8) & 0xFF;
                amplitude[2, X] = (Constants.PhonemeAmplitudesTable[Y] >> 16) & 0xFF;
                Inc(ref X);
                Dec(ref phase2);
            } while (phase2 != 0);
            Inc(ref mem44);
        } while (mem44 != 0);

        X = mem44 = mem49 = 0;
        while (true)
        {
            Y = IndexOutput[X];
            A = IndexOutput[X + 1];

            if (A == 255) break;

            mem56 = Constants.BlendRank[X = A];
            A = Constants.BlendRank[Y];

            if (A == mem56)
            {
                phase1 = Constants.OutBlend[Y];
                phase2 = Constants.OutBlend[X];
            }
            else if (A < mem56)
            {
                phase1 = Constants.InBlend[X];
                phase2 = Constants.OutBlend[X];
            }
            else
            {
                phase1 = Constants.OutBlend[Y];
                phase2 = Constants.InBlend[Y];
            }

            Y = mem44;
            mem49 = A = mem49 + LengthOutput[mem44];
            A += phase2;
            speedcounter = A;
            mem47 = 168;
            phase3 = mem49 - phase1;
            mem38 = A = phase1 + phase2;

            X = A - 2;
            if ((X & 128) == 0)
                do
                {
                    mem40 = mem38;
                    if (mem47 == 168)
                    {
                        int mem36 = LengthOutput[mem44] >> 1;
                        int mem37 = LengthOutput[mem44 + 1] >> 1;
                        mem40 = mem36 + mem37;
                        mem37 += mem49;
                        mem36 = mem49 - mem36;
                        A = Read(mem47, mem37);
                        Y = mem36;
                        mem53 = A - Read(mem47, mem36);
                    }
                    else
                    {
                        A = Read(mem47, speedcounter);
                        Y = phase3;
                        mem53 = A - Read(mem47, phase3);
                    }

                    mem50 = mem53 & 128;
                    int m53abs = Mathf.Abs(mem53);
                    mem51 = m53abs % mem40;
                    mem53 /= mem40;
                    X = mem40;
                    Y = phase3;
                    mem56 = 0;

                    while (true)
                    {
                        mem48 = A = Read(mem47, Y) + mem53; ;
                        Inc(ref Y);
                        Dec(ref X);
                        if (X == 0) break;
                        mem56 += mem51;
                        if (mem56 >= mem40)
                        {
                            mem56 -= mem40;
                            if ((mem50 & 128) == 0)
                            {
                                if (mem48 != 0) Inc(ref mem48);
                            }
                            else Dec(ref mem48);
                        }
                        Write(mem47, Y, mem48);
                    }
                    Inc(ref mem47);
                } while (mem47 != 175);
            Inc(ref mem44);
            X = mem44;
        }

        mem48 = mem49 + LengthOutput[mem44];
        for (int i = 0; i < 256; i++) pitches[i] -= (frequency[0, i] >> 1); // signmode was here

        phase1 = phase2 = phase3 = mem49 = 0;
        speedcounter = 72; // sam standard speed

        for (int i = 255; i >= 0; i--)
        {
            amplitude[0, i] = Constants.AmplitudeRescale[amplitude[0, i]];
            amplitude[1, i] = Constants.AmplitudeRescale[amplitude[1, i]];
            amplitude[2, i] = Constants.AmplitudeRescale[amplitude[2, i]];
        }

        Y = 0;
        mem44 = A = X = pitches[0];
        mem38 = A - (A >> 2);

        bool unknownBool = false;
        while (true)
        {
            mem39 = A = sampledConsonantFlag[Y];

            A &= 248;
            if (A != 0)
            {
                RenderSample(ref mem66);
                Y += 2;
                mem48 -= 2;
            }
            else
            {
                int[] ary = new int[5];
                int p1 = phase1 * 256;
                int p2 = phase2 * 256;
                int p3 = phase3 * 256;
                for (int k = 0; k < 5; k++)
                {
                    int sp1 = Constants.Sinus[0xff & (p1 >> 8)];
                    int sp2 = Constants.Sinus[0xff & (p2 >> 8)];
                    int rp3 = ((p3 >> 8) & 0xff) <= 127 ? 0x90 : 0x70;
                    int sin1 = sp1 * (amplitude[0, Y] & 0x0f);
                    int sin2 = sp2 * (amplitude[1, Y] & 0x0f);
                    int rect = rp3 * (amplitude[2, Y] & 0x0f);
                    ary[k] = ((sin1 + sin2 + rect) / 32 + 128) & 255;
                    p1 += frequency[0, Y] * 64;
                    p2 += frequency[1, Y] * 64;
                    p3 += frequency[2, Y] * 64;
                }

                Sam.Buffer.WriteArray(0, ary);
                Dec(ref speedcounter);
                if (speedcounter != 0) goto pos48155;
                Inc(ref Y);
                Dec(ref mem48);
            }

            if (mem48 == 0) return;
            speedcounter = Sam.Speed;

        pos48155:
            Dec(ref mem44);

        pos48159:
            if (mem44 == 0 || unknownBool)
            {
                unknownBool = false;
                mem44 = A = pitches[Y];
                mem38 = A -= (A >> 2);
                phase1 = phase2 = phase3 = 0;
                continue;
            }
            Dec(ref mem38);
            if ((mem38 != 0) || (mem39 == 0))
            {
                phase1 = (phase1 + frequency[0, Y]) & 255;
                phase2 = (phase2 + frequency[1, Y]) & 255;
                phase3 = (phase3 + frequency[2, Y]) & 255;
                continue;
            }
            RenderSample(ref mem66);
            unknownBool = true;
            goto pos48159;
        }
    }

    #endregion
}
