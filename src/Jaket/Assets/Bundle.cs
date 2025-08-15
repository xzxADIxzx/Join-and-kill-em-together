namespace Jaket.Assets;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Jaket.IO;
using Jaket.UI;
using Jaket.UI.Dialogs;

using static Jaket.UI.Lib.Pal;

/// <summary> I18n bundle that loads translated lines from a property file corresponding to the selected language. </summary>
public static class Bundle
{
    /// <summary> Language codes for internal use. </summary>
    public static readonly string[] Codes = { "ar", "pt", "en", "fl", "fr", "it", "pl", "ru", "es", "uk" };
    /// <summary> Language names to display. </summary>
    public static readonly string[] Locales = { "عربي", "Português brasileiro", "English", "Filipino", "Français", "Italiano", "Polski", "Русский", "Español", "Українська" };
    /// <summary> Property files containing localization. </summary>
    public static readonly string[] Content = { "arabic", "brazilianportuguese", "english", "filipino", "french", "italian", "polish", "russian", "spanish", "ukrainian" };

    /// <summary> Identifier of the loaded localization or -1 if the bundle is not loaded yet. </summary>
    public static int Loaded = -1;
    /// <summary> Map of translated lines by their keys. </summary>
    private static Dictionary<string, string> lines = new();
    /// <summary> Text to show in the hud after scene loading. </summary>
    private static string text2Show;

    /// <summary> Loads the translation specified in the settings. </summary>
    public static void Load()
    {
        Events.OnLoad += () =>
        {
            if (text2Show == null) return;

            HudMessageReceiver.Instance?.SendHudMessage(text2Show);
            text2Show = null;
        };

        Files.MakeDir(Files.Bundles);
        Files.MoveAll(Files.Root, Files.Bundles, "*.properties");

        var locale = Settings.Locale;
        int localeId = Codes.IndexOf(locale);

        if (localeId == -1)
        {
            Log.Error($"[BNDL] Couldn't find a bundle for the {locale} language code");
            return;
        }

        string file = Files.Join(Files.Bundles, $"{Content[localeId]}.properties");
        string[] content;

        try
        {
            content = Files.ReadLines(file);
        }
        catch (Exception ex)
        {
            Log.Error($"[BNDL] Couldn't find the bundle file; the path is {file}", ex);
            return;
        }

        foreach (var line in content)
        {
            // skip comments and blank lines
            if (string.IsNullOrWhiteSpace(line) || line[0] == '#') continue;

            var pair = line.Split('=');
            lines.Add(pair[0].Trim(), Parse(pair[1].Trim()));
        }

        Loaded = localeId;
        Log.Info($"[BNDL] Loaded {lines.Count} lines of {Locales[localeId]} ({locale}) locale");
    }

    #region parsing

    /// <summary> Returns a string without Unity and Jaket formatting. </summary>
    public static string CutColors(string original) => Regex.Replace(original, "<.*?>|\\[.*?\\]", string.Empty);

    /// <summary> Returns a string without the tags that can cause lags. </summary>
    public static string CutDanger(string original) => Regex.Replace(original, "</?b>|</?i>|</?color.*?>|</?size.*?>|</?quad.*?>|</?material.*?>|\\\\n|\n", string.Empty);

    /// <summary> Parses the formatting tags in the given string so that Unity can understand them. </summary>
    public static string Parse(string original, int maxSize = 64)
    {
        Stack<int> types = new();
        string Closing() => types.Pop() switch { 0 => "</color>", 1 => "</size>", 2 => "</b>", _ => "</i>" };

        StringBuilder builder = new(original.Length);
        int pointer = 0;

        // backslash-ns are read as regular text, so they must be manually replaced with the newline character
        // the space and backslash are needed to prevent out of bounds
        original = $" {original.Replace("\\n", "\n")}\\";

        while (pointer < original.Length)
        {
            // find the index of the next special character
            int old = pointer;
            pointer = original.IndexOfAny(new[] { '\\', '[' }, pointer);

            // save the piece between the previous pointer and the special character
            builder.Append(original[old..pointer]);

            // process the special char
            char c = original[pointer];

            if (c == '\\') pointer++;
            if (c == '[')
            {
                if (original[pointer - 1] == '\\')
                {
                    builder.Append('[');
                    pointer++;
                }
                else if (original[pointer + 1] == ']')
                {
                    builder.Append(types.Count > 0 ? Closing() : "[]");
                    pointer += 2;
                }
                else
                {
                    old = ++pointer;
                    pointer = original.IndexOf(']', pointer);

                    var tag = original[old..pointer++];

                    if (int.TryParse(tag, out int size))
                    {
                        types.Push(1);
                        builder.Append("<size=").Append(Math.Min(size, maxSize)).Append(">");
                    }
                    else if (tag == "b" || tag == "bold")
                    {
                        types.Push(2);
                        builder.Append("<b>");
                    }
                    else if (tag == "i" || tag == "italic")
                    {
                        types.Push(3);
                        builder.Append("<i>");
                    }
                    else if (tag.Length >= 3 && tag[0] == '#')
                    {
                        types.Push(0);
                        builder.Append("<color=").Append(tag).Append(">");
                    }
                    else if (tag.Length >= 3 && Colors[Hash(tag)] != null)
                    {
                        types.Push(0);
                        builder.Append("<color=").Append(Colors[Hash(tag)]).Append(">");
                    }
                }
            }
        }

        // close the remaining tags
        while (types.Count > 0) builder.Append(Closing());

        return builder.Remove(0, 1).Replace(":heart:", "♡").ToString();
    }

    #endregion
    #region usage

    /// <summary> Returns a localized line by the key. </summary>
    public static string Get(string key, string fallback = "OH NO") => lines.TryGetValue(key, out var line) ? line : fallback;

    /// <summary> Returns a localized & formatted line by the key. </summary>
    public static string Format(string key, params string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i][0] == '#') args[i] = Get(args[i][1..], args[i]);
        }
        return string.Format(Get(key), args);
    }

    /// <summary> Sends a localized message to the hud. </summary>
    public static void Hud(string key, bool silent = false) => HudMessageReceiver.Instance?.SendHudMessage(Get(key), silent: silent);

    /// <summary> Sends a localized & formatted message to the hud. </summary>
    public static void Hud(string key, bool silent, params string[] args) => HudMessageReceiver.Instance?.SendHudMessage(Format(key, args), silent: silent);

    /// <summary> Sends a localized message to the hud after scene loading. </summary>
    public static void Hud2NS(string key) => text2Show = Get(key);

    /// <summary> Sends a localized & formatted message to the hud after scene loading. </summary>
    public static void Hud2NS(string key, params string[] args) => text2Show = Format(key, args);

    /// <summary> Sends a localized message to the chat. </summary>
    public static void Msg(string key) => UI.Chat.Receive(Get(key), false);

    /// <summary> Sends a localized & formatted message to the chat. </summary>
    public static void Msg(string key, params string[] args) => UI.Chat.Receive(Format(key, args), false);

    #endregion
}
