namespace Jaket;

using BepInEx.Bootstrap;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Networking;

using Jaket.Assets;

/// <summary> Class responsible for interactions with the repository of the project. </summary>
public static partial class Version
{
    /// <summary> Repository of the project. </summary>
    public const string REPO = "xzxADIxzx/Join-and-kill-em-together";
    /// <summary> Various API access points. </summary>
    public const string GITHUB_API = "https://api.github.com", GITHUB_RAW = "https://raw.githubusercontent.com";

    /// <summary> Readable version of the project. </summary>
    public static string Readable => DEBUG ? $"{CURRENT}-beta" : CURRENT;
    /// <summary> Hash of the commit of the build. </summary>
    public static string Hash => Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion[^40..^32];

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
            const string TAG = "\"tag_name\": \"V";
            const string NAME = "\"name\": \"";

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
        code => Log.Info($"[UPDT] {(code == 200L ? "Fetched" : "Couldn't fetch")} the latest version of the project"));
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
        },
        code =>
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
