namespace Jaket;

using BepInEx.Bootstrap;
using System.Collections.Generic;
using UnityEngine.Networking;

using Jaket.Assets;

/// <summary> Class that handles interactions with the repository of the mod. This includes checking for updates and fetching compatible mods. </summary>
public static class Version
{
    /// <summary> Current version of the project. </summary>
    public const string CURRENT = "2.0.0";
    /// <summary> Whether current build is debug. </summary>
    public const bool DEBUG = true;

    /// <summary> Repository of the project to fetch the data from. </summary>
    public const string REPO = "xzxADIxzx/Join-and-kill-em-together";
    /// <summary> Json fragments preceding the name and tag of the latest version. </summary>
    public const string TAG = "\"tag_name\": \"V", NAME = "\"name\": \"";

    public const string GITHUB_API = "https://api.github.com";
    public const string GITHUB_RAW = "https://raw.githubusercontent.com";

    /// <summary> List of mods compatible with Jaket. </summary>
    public static string[] Compatible = { "Jaket" };
    /// <summary> Whether at least one incompatible mod is loaded. </summary>
    public static bool HasIncompatibility;

    /// <summary> Fetches data from the given URL address. </summary>
    public static void Fetch(string url, Cons<string> cons, Cons<long> pst) => UnityWebRequest.Get(url).SendWebRequest().completed += t =>
    {
        var r = (t as UnityWebRequestAsyncOperation).webRequest;
        if (r.responseCode == 200) cons(r.downloadHandler.text);
        pst(r.responseCode);
    };

    /// <summary> Fetches the latest version from GitHub and notifies players about it so that they no longer whine to me. </summary>
    public static void Check4Updates()
    {
        Log.Info("[UPDT] Checking for updates...");
        Fetch($"{GITHUB_API}/repos/{REPO}/releases/latest", res =>
        {
            int tagIndex = res.IndexOf(TAG), nameIndex = res.IndexOf(NAME);
            if (tagIndex == -1 || nameIndex == -1)
            {
                Log.Warning("[UPDT] Couldn't extract the latest version of the project.");
                return;
            }

            var latest = res.Substring(tagIndex += TAG.Length, res.IndexOf('"', tagIndex) - tagIndex);
            var name = res.Substring(nameIndex += NAME.Length, res.IndexOf('"', nameIndex) - nameIndex);

            if (latest != CURRENT) Bundle.Hud("outdated-mod", false, CURRENT, latest, name);
        },
        code => Log.Info($"[UPDT] {(code == 200 ? "Fetched" : "Couldn't fetch")} the latest version of the project"));
    }

    /// <summary> Fetches the list of mods that are compatible with Jaket from GitHub. </summary>
    public static void FetchCompatible()
    {
        Log.Info("[UPDT] Fetching the list of compatible mods...");
        Fetch($"{GITHUB_RAW}/{REPO}/refs/heads/main/compatible-mods.json", res =>
        {
            Compatible = new string[res.Count(c => c == '"') / 2];

            for (int i = 0, s = 0, e = 0; i < Compatible.Length; i++)
            {
                s = res.IndexOf('"', e + 1) + 1;
                e = res.IndexOf('"', s);

                Compatible[i] = res[s..e];
            }
        }, code =>
        {
            List<string> incompatible = new();
            Chainloader.PluginInfos.Keys.Each(p => Compatible.All(c => c != p), incompatible.Add);

            if (HasIncompatibility = incompatible.Count > 0)
                Log.Warning($"[UPDT] Incompatible mods found: {string.Join(", ", incompatible)}");
            else
                Log.Info("[UPDT] All of the loaded mods are compatible with the project");
        });
    }
}
