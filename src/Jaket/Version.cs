namespace Jaket;

using BepInEx.Bootstrap;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

using Jaket.Assets;
using Jaket.UI;

using static Jaket.UI.Rect;

// TODO docs
public class Version
{
    /// <summary> Current version of the mod installed by the player. </summary>
    public const string CURRENT = "1.3.42";
    /// <summary> Repository of the mod, where the newest version will be taken from. </summary>
    public const string REPO = "xzxADIxzx/Join-and-kill-em-together";
    /// <summary> Json fragments preceding a tag and a name of the latest version of the mod. </summary>
    public const string TAG = "\"tag_name\": \"V", NAME = "\"name\": \"";

    public const string GITHUB_API = "https://api.github.com";
    public const string GITHUB_RAW = "https://raw.githubusercontent.com";

    /// <summary> List of mods compatible with Jaket. </summary>
    public static string[] Compatible = { "Jaket" };
    /// <summary> Whether at least one incompatible mod is loaded. </summary>
    public static bool HasIncompatibility;

    #region version

    /// <summary> Checks for updates using GitHub and notifies the player about it. </summary>
    public static void Check4Update() => Fetch((done, result) =>
    {
        if (done && Parse(result, out var latest, out var name) && latest != CURRENT) Bundle.Hud("version.outdated", false, CURRENT, latest, name);
    });

    /// <summary> Fetches a json file with all versions of the mod from GitHub. </summary>
    public static void Fetch(Cons<bool, string> result)
    {
        Log.Info("Checking for updates...");
        var request = UnityWebRequest.Get($"{GITHUB_API}/repos/{REPO}/releases");
        request.SendWebRequest().completed += _ => result(request.isDone, request.downloadHandler.text);
    }

    /// <summary> Extracts the tag and name of the latest version of the mod from the given json file. </summary>
    public static bool Parse(string result, out string latest, out string name)
    {
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

    #endregion
    #region compatibility

    /// <summary> Fetches a json file with all mods that are compatible with Jaket. </summary>
    public static void FetchCompatible()
    {
        Log.Info("Fetching the list of compatible mods...");
        var request = UnityWebRequest.Get($"{GITHUB_RAW}/{REPO}/refs/heads/main/compatible-mods.json");
        request.SendWebRequest().completed += _ =>
        {
            if (request.isDone && (request.responseCode == 0 || request.responseCode == 200)) Parse(request.downloadHandler.text);

            HasIncompatibility = Chainloader.PluginInfos.Values.Any(info => !Compatible.Contains(info.Metadata.Name));
        };
    }

    /// <summary> Extracts the list of compatible mods from the given json file. </summary>
    public static void Parse(string result)
    {
        Compatible = new string[result.Count(c => c == '\n') - 2];

        for (int i = 0, s = 0, e = 0; i < Compatible.Length; i++)
        {
            s = result.IndexOf('"', e + 1) + 1;
            e = result.IndexOf('"', s);

            Compatible[i] = result.Substring(s, e - s);
        }
    }

    #endregion
}
