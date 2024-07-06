namespace Jaket;

using System;
using UnityEngine;
using UnityEngine.Networking;

using Jaket.Assets;
using Jaket.UI;

using static Jaket.UI.Rect;

public class Version
{
    /// <summary> Current version of the mod installed by the player. </summary>
    public const string CURRENT = "1.3.0";
    /// <summary> Repository of the mod, where the newest version will be taken from. </summary>
    public const string REPO = "xzxADIxzx/Join-and-kill-em-together";
    /// <summary> Github API URL. I think it's not difficult to guess. </summary>
    public const string GITHUB_API = "https://api.github.com";
    /// <summary> Json fragments preceding a tag and a name of the latest version of the mod. </summary>
    public const string TAG = "\"tag_name\": \"V", NAME = "\"name\": \"";

    /// <summary> Notifies the player that their version of the mod doesn't match the host's one. </summary>
    public static void Notify() => Bundle.Hud("version.host-outdated");

    /// <summary> Checks for updates using Github and notifies the player about it. </summary>
    public static void Check4Update() => Fetch((done, result) =>
    {
        if (done && Parse(result, out var latest, out var name) && latest != CURRENT) Bundle.Hud("version.outdated", false, CURRENT, latest, name);
    });

    /// <summary> Fetches a json file with all versions of the mod from GitHub. </summary>
    public static void Fetch(Action<bool, string> result)
    {
        Log.Info("Checking for updates...");
        var request = UnityWebRequest.Get($"{GITHUB_API}/repos/{REPO}/releases");
        request.SendWebRequest().completed += _ => result(request.isDone, request.downloadHandler.text);
    }

    /// <summary> Extracts a tag and a name of the latest version of the mod from a json file. </summary>
    public static bool Parse(string result, out string latest, out string name)
    {
        // default value with sad emoji
        latest = name = "Failed to parse data ;(";

        int tagIndex = result.IndexOf(TAG), nameIndex = result.IndexOf(NAME);
        if (tagIndex == -1 || nameIndex == -1) return false;

        latest = result.Substring(tagIndex += TAG.Length, result.IndexOf('"', tagIndex) - tagIndex);
        name = result.Substring(nameIndex += NAME.Length, result.IndexOf('"', nameIndex) - nameIndex);

        return true;
    }

    /// <summary> Adds the mod version to the bottom left edge of the screen. </summary>
    public static void Label(Transform parent)
    {
        var r = Blw(36f, 40f);
        UIB.Table("Version", parent, r, table => UIB.Text($"Jaket version is {CURRENT}", table, r.Text, Color.grey));
    }
}