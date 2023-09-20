namespace Jaket.Sam;

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

        int mem56 = 255, mem57, mem58, mem59, mem60, mem61 = 255, mem62, mem64 = 0, mem65, mem66, mem36653;

        temp = new int[256];
        temp[0] = 32;

        X = 1;
        Y = 0;
        do
        {
            A = input[Y] & 127;
            if (A >= 112) A = A & 95;
            else if (A >= 96) A = A & 79;

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
                System.Array.Copy(input, copy, X);
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
            // A = '.';
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
        mem36653 = A;
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
            A = A & 127;
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
        A = A & 128;
        if (A != 0) goto pos36700;
        pos36905:
        mem59 = X;
        goto pos36791;

    pos36910:
        UnknownCode1(mem59);
        A = A & 64;
        if (A != 0) goto pos36905;
        goto pos36700;

    pos36920:
        UnknownCode1(mem59);
        A = A & 8;
        if (A == 0) goto pos36700;
        pos36930:
        mem59 = X;
        goto pos36791;

    pos36935:
        UnknownCode1(mem59);
        A = A & 16;
        if (A != 0) goto pos36930;
        A = temp[X];
        if (A != 72) goto pos36700;
        Dec(ref X);
        A = temp[X];
        if ((A == 67) || (A == 83)) goto pos36930;
        goto pos36700;

    pos36967:
        UnknownCode1(mem59);
        A = A & 4;
        if (A != 0) goto pos36930;
        A = temp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem59 = X;
        goto pos36791;


    pos37004:
        UnknownCode1(mem59);
        A = A & 32;
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
        A = A & 32;
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
        A = A & 128;
        if (A != 0) goto pos36700;
        pos37305:
        mem58 = X;
        goto pos37184;

    pos37310:
        UnknownCode2(mem58);
        A = A & 64;
        if (A != 0) goto pos37305;
        goto pos36700;

    pos37320:
        UnknownCode2(mem58);
        A = A & 8;
        if (A == 0) goto pos36700;
        pos37330:
        mem58 = X;
        goto pos37184;

    pos37335:
        UnknownCode2(mem58);
        A = A & 16;
        if (A != 0) goto pos37330;
        A = temp[X];
        if (A != 72) goto pos36700;
        Inc(ref X);
        A = temp[X];
        if ((A == 67) || (A == 83)) goto pos37330;
        goto pos36700;

    pos37367:
        UnknownCode2(mem58);
        A = A & 4;
        if (A != 0) goto pos37330;
        A = temp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem58 = X;
        goto pos37184;

    pos37404:
        UnknownCode2(mem58);
        A = A & 32;
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
        A = A & 32;
        if (A == 0) goto pos37184;
        mem58 = X;
        goto pos37440;

    pos37455:
        Y = mem64;
        mem61 = mem60;

    pos37461:
        A = GetRuleByte(mem62, Y);
        mem57 = A;
        A = A & 127;
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

    public int[] StressOutput = new int[256];
    public int[] LengthOutput = new int[256];
    public int[] IndexOutput = new int[256];

    public void Render() { }

    public bool TextToPhonemes(ref int[] input) => true;
}
