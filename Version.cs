namespace Jaket;

using System;
using UnityEngine.Networking;

public class Version
{
    /// <summary> Current version of the mod installed by the player. </summary>
    public const string CURRENT = "0.8.12";
    /// <summary> Repository of the mod, where the newest version will be taken from. </summary>
    public const string REPO = "xzxADIxzx/Join-and-kill-em-together";
    /// <summary> Github API URL. I think it's not difficult to guess. </summary>
    public const string GITHUB_API = "https://api.github.com";
    /// <summary> Json fragments preceding a tag and a name of the latest version of the mod. </summary>
    public const string TAG = "\"tag_name\": \"V", NAME = "\"name\": \"";

    /// <summary> Notifies the player that their version of the mod is outdated. </summary>
    public static void Notify(string latest, string name) => HudMessageReceiver.Instance.SendHudMessage(
$@"Your version of the Jaket mod is <color=orange>outdated</color>!
Update the poor little mod <color=#FF66FF>please</color> :3
<size=20><color=grey>{CURRENT} -> {latest} {name}</color></size>");

    /// <summary> Notifies the player that their version of the mod doesn't match the host's one. </summary>
    public static void NotifyHost() => HudMessageReceiver.Instance.SendHudMessage(
$@"<size=20>Your version of the Jaket mod doesn't match the host's one!</size>
This may lead to <color=orange>dire consequences</color> D:");

    /// <summary> Checks for updates using Github and notifies the player about it. </summary>
    public static void Check4Update() => Fetch((done, result) =>
    {
        if (done && Parse(result, out var latest, out var name) && latest != CURRENT) Notify(latest, name);
    });

    /// <summary> Fetches a json file with all versions of the mod from GitHub. </summary>
    public static void Fetch(Action<bool, string> result)
    {
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
}