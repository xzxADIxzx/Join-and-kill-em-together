namespace Jaket.Assets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/// <summary> Class that loads translations from files in the bundles folder and returns translated lines by keys. </summary>
public class Bundle
{
    /// <summary> Language codes used in settings. </summary>
    public static readonly string[] Codes = { "pt", "en", "fl", "fr", "it", "pl", "ru", "es", "uk" };
    /// <summary> Displayed language name so that everyone can find out their own even without knowledge of English. </summary>
    public static readonly string[] Locales = { "Português brasileiro", "English", "Filipino", "Français", "Italiano", "Polski", "Русский", "Español", "Українська" };
    /// <summary> File names containing localization. </summary>
    public static readonly string[] Files = { "brazilianportuguese", "english", "filipino", "french", "italian", "polish", "russian", "spanish", "ukrainian" };

    /// <summary> Id of loaded localization. -1 if the localization is not loaded yet. </summary>
    public static int LoadedLocale = -1;
    /// <summary> Dictionary with all lines of loaded localization. </summary>
    private static Dictionary<string, string> props = new();

    /// <summary> Loads the translation specified in the settings. </summary>
    public static void Load()
    {
        var locale = PrefsManager.Instance.GetString("jaket.locale", "en");
        int localeId = Array.IndexOf(Codes, locale);

        if (localeId == 255)
        {
            Log.Error($"Couldn't find the bundle for {locale} language code!");
            return;
        }

        var file = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "bundles", $"{Files[localeId]}.properties");
        string[] lines;
        try
        {
            lines = File.ReadAllLines(file);
        }
        catch (Exception ex)
        {
            Log.Error(new IOException($"Couldn't find the bundle file; path is {file}", ex));
            return;
        }

        foreach (var line in lines)
        {
            // skip comments and blank lines
            if (line == "" || line.StartsWith("#")) continue;

            var pair = line.Split('=');
            props.Add(pair[0].Trim(), ParseColors(pair[1].Trim()));
        }

        LoadedLocale = localeId;
        Log.Info($"Loaded {props.Count} lines of {Locales[localeId]} ({locale}) locale");
    }

    #region parsing

    // <summary> Returns a string without Unity formatting. </summary>
    public static string CutColors(string original) => Regex.Replace(original, "<.*?>", string.Empty);

    // <summary> Returns the length of the string without Unity formatting. </summary>
    public static int RawLength(string original) => CutColors(original).Length;

    /// <summary> Parses the colors in the given string so that Unity could understand them. </summary>
    public static string ParseColors(string original)
    {
        Stack<bool> types = new(); // true - font size, false - color
        StringBuilder builder = new(original.Length);
        int pointer = 0;

        // \n is read as a regular text, so it must be manually replaced with a transfer char
        // space and \ are needed to prevent OutOfBounds
        original = $" {original.Replace("\\n", "\n")}\\";

        while (pointer < original.Length)
        {
            // find the index of the next special char
            int old = pointer;
            pointer = original.IndexOfAny(new[] { '\\', '[', ']' }, pointer);

            // save a piece of the original line without special characters
            builder.Append(original.Substring(old, pointer - old));

            // process the special char
            char c = original[pointer];

            if (c == '\\') pointer++;
            else if (c == '[')
            {
                if (original[pointer - 1] == '\\')
                {
                    builder.Append('[');
                    pointer++;
                }
                else if (original[pointer + 1] == ']')
                {
                    builder.Append(types.Pop() ? "</size>" : "</color>");
                    pointer += 2;
                }
                else
                {
                    old = ++pointer;
                    pointer = original.IndexOf(']', pointer);

                    var content = original.Substring(old, pointer - old);
                    bool isSize = int.TryParse(content, out _);

                    types.Push(isSize);
                    builder.Append(isSize ? "<size=" : "<color=").Append(content).Append('>');
                    pointer++;
                }
            }
            else if (c == ']')
            {
                builder.Append(']');
                pointer++;
            }
        }

        // just in case
        foreach (var size in types) builder.Append(size ? "</size>" : "</color>");

        return builder.ToString().Substring(1);
    }

    #endregion
    #region usage

    /// <summary> Returns a localized line by the key. </summary>
    public static string Get(string key, string fallback = "OH NO") => props.TryGetValue(key, out var line) ? line : fallback;

    /// <summary> Returns a localized & formatted line by the key. </summary>
    public static string Format(string key, params string[] args)
    {
        for (int i = 0; i < args.Length; i++)
            if (args[i].StartsWith("#")) args[i] = Get(args[i].Substring(1), args[i]);

        return string.Format(Get(key), args);
    }

    /// <summary> Sends a localized message to the HUD. </summary>
    public static void Hud(string key, bool silent = false) => HudMessageReceiver.Instance?.SendHudMessage(Get(key), silent: silent);

    /// <summary> Sends a localized & formatted message to the HUD. </summary>
    public static void Hud(string key, bool silent, params string[] args) => HudMessageReceiver.Instance?.SendHudMessage(Format(key, args), silent: silent);

    #endregion
}
