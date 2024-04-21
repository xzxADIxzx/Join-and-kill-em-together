namespace Jaket.Assets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Jaket.UI.Dialogs;

/// <summary> Class that loads translations from files in the bundles folder and returns translated lines by keys. </summary>
public class Bundle
{
    /// <summary> Language codes used in settings. </summary>
    public static readonly string[] Codes = { "ar", "pt", "en", "fl", "fr", "it", "pl", "ru", "es", "uk" };
    /// <summary> Displayed language name so that everyone can find out their own even without knowledge of English. </summary>
    public static readonly string[] Locales = { "عربي", "Português brasileiro", "English", "Filipino", "Français", "Italiano", "Polski", "Русский", "Español", "Українська" };
    /// <summary> File names containing localization. </summary>
    public static readonly string[] Files = { "arabic", "brazilianportuguese", "english", "filipino", "french", "italian", "polish", "russian", "spanish", "ukrainian" };

    /// <summary> Id of loaded localization. -1 if the localization is not loaded yet. </summary>
    public static int LoadedLocale = -1;
    /// <summary> Dictionary with all lines of loaded localization. </summary>
    private static Dictionary<string, string> props = new();

    /// <summary> Loads the translation specified in the settings. </summary>
    public static void Load()
    {
        var root = Path.GetDirectoryName(Plugin.Instance.Location);
        #region r2mm fix

        var bundles = Path.Combine(root, "bundles");
        if (!Directory.Exists(bundles)) Directory.CreateDirectory(bundles);

        foreach (var prop in Directory.EnumerateFiles(root, "*.properties"))
        {
            var dest = Path.Combine(bundles, Path.GetFileName(prop));

            File.Delete(dest);
            File.Move(prop, dest);
        }

        #endregion

        var locale = PrefsManager.Instance.GetString("jaket.locale", "en");
        int localeId = Array.IndexOf(Codes, locale);

        if (localeId == 255)
        {
            Log.Error($"Couldn't find the bundle for {locale} language code!");
            return;
        }

        var file = Path.Combine(root, "bundles", $"{Files[localeId]}.properties");
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
            props.Add(pair[0].Trim(), locale == "ar" ? ParseArabic(pair[1].Trim()) : ParseColors(pair[1].Trim()));
        }

        LoadedLocale = localeId;
        Log.Info($"Loaded {props.Count} lines of {Locales[localeId]} ({locale}) locale");
    }

    #region parsing

    // <summary> Returns a string without Unity and Jaket formatting. </summary>
    public static string CutColors(string original) => Regex.Replace(original, "<.*?>|\\[.*?\\]", string.Empty);

    // <summary> Returns a string without the tags that can cause lags. </summary>
    public static string CutDangerous(string original) => Regex.Replace(original, "</?size.*?>|</?quad.*?>|</?material.*?>", string.Empty);

    /// <summary> Parses the colors in the given string so that Unity could understand them. </summary>
    public static string ParseColors(string original, int maxSize = 64)
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
            pointer = original.IndexOfAny(new[] { '\\', '[' }, pointer);

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
                    bool isSize = int.TryParse(content, out var size);

                    types.Push(isSize);
                    builder.Append(isSize ? "<size=" : "<color=").Append(isSize ? Math.Min(size, maxSize) : content).Append('>');
                    pointer++;
                }
            }
        }

        // just in case
        foreach (var size in types) builder.Append(size ? "</size>" : "</color>");

        return builder.ToString().Substring(1);
    }

    /// <summary> Reverses the string because Arabic is right-to-left language. </summary>
    public static string ParseArabic(string original) => new(CutColors(original).Replace("\\n", "\n").Replace('{', '#').Replace('}', '{').Replace('#', '}').Reverse().ToArray());

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

    /// <summary> Sends a localized message to the chat. </summary>
    public static void Msg(string key) => Chat.Instance.Receive(Get(key), format: false);

    /// <summary> Sends a localized & formatted message to the chat. </summary>
    public static void Msg(string key, params string[] args) => Chat.Instance.Receive(Format(key, args), format: false);

    #endregion
}
