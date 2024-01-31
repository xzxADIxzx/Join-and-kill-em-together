namespace Jaket;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Steamworks;
using UnityEngine;

public class CachedSpray
{
    /// <summary> Maximum size of the spray in bytes. </summary>
    public const int MaxSize = 512 * 1024;
    public SteamId Owner;
    public Texture2D Texture;

    public CachedSpray(SteamId owner, byte[] bytes)
    {
        Owner = owner;
        Texture = GetTexture(bytes);
    }

    /// <summary> Converts the bytes to a texture. </summary>
    public static Texture2D GetTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        return texture;
    }
}

/// <summary> Manages the sprays of the player and loading them. </summary>
public class SprayManager
{
    /// <summary> List of supported image extensions. </summary>
    public readonly static string[] SupportedTypes = { "png", "jpg", "jpeg" };

    /// <summary> Directory, where the mod is located. </summary>
    public static string ModDirectory;
    /// <summary> Directory, where the sprays are located. </summary>
    public static string SpraysPath;
    /// <summary> Directory, where the cached sprays are located. </summary>
    public static string CachedSpraysPath;

    public static string CurrentSprayFilePath;
    public static Texture2D CurrentSprayTexture;
    /// <summary> List of cached sprays that loaded from server. </summary>
    public static List<CachedSpray> CachedSprays = new();

    public static void Load()
    {
        // paths to the required directories
        ModDirectory = Path.GetDirectoryName(Plugin.Instance.Info.Location);
        SpraysPath = Path.Combine(ModDirectory, "sprays");

        var files = Directory.GetFiles(SpraysPath);
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).Substring(1); // remove dot
            // check if file is image, if not, skip
            if (!SupportedTypes.Contains(ext)) continue;

            // set the current spray path
            CurrentSprayFilePath = Path.Combine(SpraysPath, file);
        }

        if (CurrentSprayFilePath != null)
        {
            Log.Info($"Spray found: {CurrentSprayFilePath}");
            LoadImageFromPath(CurrentSprayFilePath);
        }
        else
            Log.Warning($"No spray found! Please add a new one or check file extensions! Supported: {string.Join(", ", SupportedTypes)}");
    }

    /// <summary> Loads the image from the given path. </summary>
    public static void LoadImageFromPath(string path = null)
    {
        var bytes = File.ReadAllBytes(path);
        // Check if the file less than 512kb
        if (bytes.Length > CachedSpray.MaxSize)
        {
            Log.Error("Spray is too big! Max 512kb only allowed!");
            return;
        }
        CurrentSprayTexture = CachedSpray.GetTexture(bytes);
        CacheSpray(SteamClient.SteamId, bytes);
    }

    /// <summary> Caches the spray in the memory. </summary>
    public static void CacheSpray(SteamId owner, byte[] bytes)
    {
        var cachedSpray = new CachedSpray(owner, bytes);
        CachedSprays.Add(cachedSpray);
    }

    /// <summary> Checks if the given owner has a cached spray. Returns CachedSpray if found, so no need to load it. </summary>
    public static CachedSpray CheckForCachedSpray(SteamId owner) => CachedSprays.Find(s => s.Owner == owner);
}