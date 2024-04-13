namespace Jaket.Sprays;

using System.Collections.Generic;
using System.Linq;
using System.IO;
using Steamworks;
using UnityEngine;

using Jaket.Net;
using Jaket.UI;
using Jaket.Content;
using Jaket.UI.Elements;

/// <summary> Manages the sprays of the player and loading them. </summary>
public class SprayManager
{
    /// <summary> List of supported image extensions. </summary>
    public readonly static string[] SupportedTypes = { "png", "jpg", "jpeg" };

    /// <summary> Directory, where the mod is located. </summary>
    public static string ModDirectory;
    /// <summary> Directory, where the sprays are located. </summary>
    public static string SpraysPath;

    /// <summary> The current client's spray. Contains the image data. </summary>
    public static SprayFile CurrentSpray;
    public static Dictionary<string, SprayFile> FileSprays = new();

    /// <summary> List of cached sprays that loaded from server. </summary>
    public static Dictionary<SteamId, CachedSpray> CachedSprays = new();
    
    /// <summary> Subscribes to various events to synchronize sprays or clear cache. </summary>
    public static void Load()
    {
        // paths to the required directories
        ModDirectory = Path.GetDirectoryName(Plugin.Instance.Info.Location);
        SpraysPath = Path.Combine(ModDirectory, "sprays");

        LoadFileSprays();
        
        // update settings and change the current spray if specified in prefs
        var ss = SpraySettings.Instance;
        ss.UpdateSettings();
        ss.ChangeSpray(ss.SelectedSpray);

        // process requests every second
        Events.EverySecond += SprayDistributor.ProcessRequests;
    }

    /// <summary> Loads the file sprays from the sprays directory. </summary>
    public static void LoadFileSprays()
    {
        FileSprays.Clear();

        var files = Directory.GetFiles(SpraysPath);
        if (files.Length == 0) // no need to load if directory is empty
        {
            Log.Warning("Sprays directory is empty");
            return;
        }

        for (int i = 0; i < Mathf.Min(files.Length, 5); i++)
        {
            var file = files[i];
            var ext = Path.GetExtension(file).Substring(1); // remove dot

            // check if file is image, if not, skip
            if (!SupportedTypes.Contains(ext)) continue;

            string filePath = Path.Combine(SpraysPath, file);
            var name = Path.GetFileName(file);
            FileSprays.Add(name, new SprayFile(name, filePath));
        }

        // print all file sprays
        Log.Debug($"Loaded {FileSprays.Count} file sprays: {string.Join(", ", FileSprays.Keys)}");
    }

    /// <summary> Sets the current file spray. Returns null if something went wrong. </summary>
    public static SprayFile SetSpray(string sprayName)
    {
        if (!FileSprays.TryGetValue(sprayName, out var fileSpray))
        {
            Log.Error($"Spray {sprayName} not found!");
            return null;
        }
        else
        {
            // just in case if someone try to set too big spray using file
            if (fileSpray.CheckSize())
            {
                Log.Error($"Spray {sprayName} is too big! ({fileSpray.ImageData.Length} bytes)");
                return null;
            }
            CurrentSpray = fileSpray;
            return CurrentSpray;   
        }
    }

    #region sprays

    /// <summary> Sends a spray placement packet. </summary>
    private static void SendSprayPlace(Vector3 position, Vector3 direction)
    {
        if (LobbyController.Lobby != null) Networking.Send(PacketType.Spray, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Vector(position);
            w.Vector(direction);
        }, size: 32);
    }

    public static void CreateClientSpray(Vector3 position, Vector3 direction)
    {
        if (CurrentSpray == null) // no sence to create empty spray
        {
            UI.SendMsg($"You have not set any sprays! Please set one! ({Settings.Settingz})");
            return;
        }

        // send spray to the clients
        SendSprayPlace(position, direction);

        // null if not cached
        var cachedSpray = CheckForCachedSpray(Networking.LocalPlayer.Id);

        var imageData = CurrentSpray.ImageData;
        if (cachedSpray != null && LobbyController.Lobby != null) // don't change the current sprays because it's cached
            imageData = cachedSpray.ImageData;

        // if no cached spray we should assign texture after creating the spray (imageData)
        // also, we don't need to request spray from host because we are the owner of the spray (see last param)
        CreateSpray(Networking.LocalPlayer.Id, position, direction, imageData, false);
    }


    /// <summary> Creates a cached spray for the specified owner at the world, with optional data and request from host flag. </summary>
    public static CachedSpray CreateSpray(SteamId owner, Vector3 position, Vector3 direction, byte[] data = null, bool requestFromHost = true)
    {
        var cachedSpray = CheckForCachedSpray(owner);
        if (cachedSpray == null)
        {
            cachedSpray = CacheSpray(owner);

            // no cached spray, so we need to request it
            // also, no need to request from host if we are the host
            if (requestFromHost && LobbyController.Lobby != null && !LobbyController.IsOwner)
                SprayDistributor.Request(owner);
        }
        else
        {
            if (cachedSpray.Spray != null)
                cachedSpray.Spray.Lifetime = 3f; // we don't want to have many sprays in the scene at once
        }

        if (SpraySettings.Instance.DisableSprays && owner != Networking.LocalPlayer.Id)
        {
            Log.Debug($"Sprays is disabled for {owner}");
            return cachedSpray;
        }

        // create the spray in scene
        var spray = Spray.Spawn(position, direction);
        cachedSpray.Spray = spray;
        if (data != null) cachedSpray.AssignData(data);
        cachedSpray.AssignCurrent(); // assign image to the spray if it's cached

        return cachedSpray;
    }

    #endregion

    #region cached sprays

    /// <summary> Caches the spray in the memory. </summary>
    public static CachedSpray CacheSpray(SteamId owner)
    {
        Log.Debug($"Caching spray for {owner}");
        var cachedSpray = new CachedSpray(owner);
        CachedSprays.Add(owner, cachedSpray);
        return cachedSpray;
    }

    /// <summary> Returns the cached spray of the owner. Returns null if not cached. </summary>
    public static CachedSpray CheckForCachedSpray(SteamId owner) => CachedSprays.TryGetValue(owner, out var cachedSpray) ? cachedSpray : null;

    /// <summary> Removes all cached sprays. Sprays need to be loaded again. </summary>
    public static void ClearCachedSprays()
    {
        Log.Debug("Clearing cached sprays");
        CachedSprays.Clear();
    }

    /// <summary> Removes the cached spray from player. </summary>
    public static void ClearCachedSpray(SteamId owner) => CachedSprays.Remove(owner);

    #endregion
}
