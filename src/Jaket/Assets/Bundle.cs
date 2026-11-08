namespace Jaket.Assets;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Jaket.IO;
using Jaket.UI;
using Jaket.UI.Dialogs;

using static Jaket.UI.Lib.Pal;

/// <summary> I18n bundle storing localized lines loaded from a property file. </summary>
public static class Bundle
{
    /// <summary> Language codes. </summary>
    public static readonly string[] Codes = { "pt", "en", "fl", "fr", "de", "it", "pl", "ru", "es", "uk" };
    /// <summary> Language names. </summary>
    public static readonly string[] Names = { "Português brasileiro", "English", "Filipino", "Français", "Deutsch", "Italiano", "Polski", "Русский", "Español", "Українська" };
    /// <summary> Property files. </summary>
    public static readonly string[] Props = { "brazilianportuguese", "english", "filipino", "french", "german", "italian", "polish", "russian", "spanish", "ukrainian" };

    /// <summary> Identifier of the loaded localization. </summary>
    public static int Loaded { get; private set; } = -1;
    /// <summary> Localized lines. </summary>
    private static Dictionary<string, string> lines = new();
    /// <summary> On loaded popup. </summary>
    private static string popup;

    /// <summary> Loads the localization specified in the settings. </summary>
    public static void Load()
    {
        Events.OnLoad += () =>
        {
            if (popup != null)
            {
                hm.SendHudMessage(popup);
                popup = null;
            }
        };

        Files.MakeDir(Files.Bundles);
        Files.MoveAll(Files.Root, Files.Bundles, "*.properties");

        var locale = Settings.Locale;
        int localeId = Codes.IndexOf(locale);

        if (localeId == -1)
        {
            Log.Error($"[BNDL] Couldn't find a bundle file for the {locale} language code");
            return;
        }

        string path = Files.Join(Files.Bundles, $"{Props[localeId]}.properties");
        string[] file;

        try
        {
            file = Files.ReadLines(path);
        }
        catch (Exception e)
        {
            Log.Error($"[BNDL] Couldn't find a bundle file at path {path}", e);
            return;
        }

        foreach (var line in file)
        {
            if (string.IsNullOrWhiteSpace(line) || line[0] == '#') continue;

            var pair = line.Split('=', 2);

            lines.Add(pair[0].Trim(), pair[1].Trim().ParseTags());
        }

        Loaded = localeId;
        Log.Info($"[BNDL] Loaded {lines.Count} lines of {Names[localeId]} ({locale}) locale");
    }

    #region tags

    /// <summary> Returns a string without any tags. </summary>
    public static string StripTags(this string original) => Regex.Replace(original, "\\\\n|\n|<.*?>|\\[.*?\\]", string.Empty);

    /// <summary> Returns a string without lag tags. </summary>
    public static string StripLags(this string original) => Regex.Replace(original, "\\\\n|\n|</?b>|</?i>|</?color.*?>|</?size.*?>|</?quad.*?>|</?material.*?>", string.Empty);

    /// <summary> Returns a string with parsed tags. </summary>
    public static string ParseTags(this string original, int minSize = 8, int maxSize = 32)
    {
        Stack<int> types = new();
        string Closing() => types.Pop() switch { 0 => "</color>", 1 => "</size>", 2 => "</b>", _ => "</i>" };

        StringBuilder builder = new(original.Length);
        int pointer = 0;

        // backslash-ns are read as regular text, so they must be manually replaced with the newline character
        // the trailing space and backslash are needed to prevent out of bounds
        original = $" {original.Replace("\\n", "\n")}\\";

        while (pointer < original.Length)
        {
            int old = pointer;
            pointer = original.IndexOfAny([ '\\', '[' ], pointer);

            // save the piece between the previous pointer and the special character
            builder.Append(original[old..pointer]);

            // process the special character
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
                        builder.Append("<size=").Append(Math.Clamp(size, minSize, maxSize)).Append(">");
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
                    else if (tag.Length >= 3 && tag[0] == '#' | tag[0] == '{')
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

        while (types.Count > 0) builder.Append(Closing());

        return builder.Remove(0, 1).Replace(":heart:", "♡").ToString();
    }

    #endregion
    #region usage

    /// <summary> Returns a localized line. </summary>
    public static string Get(string key, string fallback = "OH NO") => lines.TryGetValue(key, out var line) ? line : fallback;

    /// <summary> Returns a formatted line. </summary>
    public static string Format(string key, params string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i][0] == '#') args[i] = Get(args[i][1..], args[i]);
        }
        return string.Format(Get(key), args);
    }

    /// <summary> Sends a localized message to the hud. </summary>
    public static void Hud(string key, bool silent = false) => hm.SendHudMessage(Get(key), silent: silent);

    /// <summary> Sends a formatted message to the hud. </summary>
    public static void Hud(string key, bool silent = false, params string[] args) => hm.SendHudMessage(Format(key, args), silent: silent);

    /// <summary> Sends a localized message as a popup. </summary>
    public static void Pop(string key) => popup = Get(key);

    /// <summary> Sends a formatted message as a popup. </summary>
    public static void Pop(string key, params string[] args) => popup = Format(key, args);

    /// <summary> Sends a localized message to the chat. </summary>
    public static void Msg(string key) => UI.Chat.Receive(Get(key), false);

    /// <summary> Sends a formatted message to the chat. </summary>
    public static void Msg(string key, params string[] args) => UI.Chat.Receive(Format(key, args), false);

    /// <summary> Sends a localized message to the chat's extra display. </summary>
    public static void Ext(string key) => UI.Chat.DisplayText(Get(key), false);

    /// <summary> Sends a formatted message to the chat's extra display. </summary>
    public static void Ext(string key, params string[] args) => UI.Chat.DisplayText(Format(key, args), false);

    #endregion
}
