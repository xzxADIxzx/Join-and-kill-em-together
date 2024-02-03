namespace Jaket;

using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Elements;
using System.Threading;
using UnityEngine.SceneManagement;

public class CachedSpray
{
    /// <summary> Maximum size of the spray in bytes. </summary>
    public const int MaxSize = 512 * 1024;
    public SteamId Owner;
    public Texture2D Texture;
    public Spray Spray;
    public byte[] ImageData;

    public CachedSpray(SteamId owner)
    {
        Owner = owner;
    }

    /// <summary> Assigns the image data to the cached spray. </summary>
    public void AssignData(byte[] data)
    {
        ImageData = data;
        Log.Info("Assigning data");
        Texture = GetTexture(data);
    }

    /// <summary> Assigns the current spray to the cached spray. </summary>
    public void AssignCurrent()
    {
        if (Spray != null && Texture != null)
            Spray.AssignImage(Texture);
    }

    /// <summary> Checks if the spray in world exists. </summary>
    public bool SprayExists() => Spray != null;

    /// <summary> Converts the bytes to a texture. </summary>
    public static Texture2D GetTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2)
        {
            filterMode = FilterMode.Point
        };
        if (bytes.Length > 0) texture.LoadImage(bytes);
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
    /// <summary> The current client's spray raw bytes texture. </summary>
    public static byte[] CurrentSpray;
    /// <summary> List of cached sprays that loaded from server. </summary>
    public static List<CachedSpray> CachedSprays = new();

    public static Dictionary<ulong, Writer> Streams = new();
    /// <summary> Size of the packet, that sends the image chunk. </summary>
    public const int ImagePacketSize = 240;

    public static void Load()
    {
        // paths to the required directories
        ModDirectory = Path.GetDirectoryName(Plugin.Instance.Info.Location);
        SpraysPath = Path.Combine(ModDirectory, "sprays");

        var files = Directory.GetFiles(SpraysPath);
        var currentSprayFilePath = string.Empty;
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).Substring(1); // remove dot
            // check if file is image, if not, skip
            if (!SupportedTypes.Contains(ext)) continue;

            // set the current spray path
            currentSprayFilePath = Path.Combine(SpraysPath, file);
        }

        if (currentSprayFilePath != null)
        {
            Log.Info($"Spray found: {currentSprayFilePath}");
            LoadImageFromPath(currentSprayFilePath);
        }
        else
            Log.Warning($"No spray found! Please add a new one or check file extensions! Supported: {string.Join(", ", SupportedTypes)}");

        // clear cached sprays, because new player has no cached sprays and sprayer doesn't send them
        // this implementation is not optimal, but it's ok for now
        SteamMatchmaking.OnLobbyMemberJoined += (_, member) => ClearCachedSprays();
        SteamMatchmaking.OnLobbyMemberLeave += (_, member) => {
            Log.Debug("Lobby member left, removing cached spray");
            ClearCachedSpray(member.Id); // also remove cached player spray because they are now worthless
        };
        SceneManager.sceneLoaded += (_, _) => ClearCachedSprays(); // remove cached sprays when scene is loaded
    }

    #region load & upload

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
        CurrentSpray = bytes;
    }

    /// <summary> Loads an image from the client. </summary>
    public static void LoadImageFromNetwork(Reader r)
    {
        var id = r.Id(); // id of the spray owner
        Log.Debug($"LOAD IMAGE, Stream value is {Streams.TryGetValue(id, out var value)} {value}");
        if (Streams.TryGetValue(id, out var stream)) // data packet
        {
            Log.Debug($"Reader size is {r.Length}, Reading {Mathf.Min(ImagePacketSize, r.Length - 9)} bytes of image data");
            stream.Bytes(r.Bytes(Mathf.Min(ImagePacketSize, r.Length - 9)));
            if (stream.Position >= stream.Length)
            {
                Log.Debug($"FINISH WITH {stream.Position} {stream.Length}");
                Reader.Read(stream.mem, stream.Length, r =>
                {
                    AssignSprayImage(id, r.Bytes(r.Length)); // image data
                });
                Marshal.FreeHGlobal(stream.mem); // free the memory allocated for image
                Streams.Remove(id);
            }
        }
        else // initial packet
        {
            if (r.Length != 13) // the client has lost the initial packet
            {
                Log.Error("Stream's initial packet was lost!");
                return;
            }

            int length = r.Int();
            Streams.Add(id, new(Marshal.AllocHGlobal(length), length));
        }
    }

    /// <summary> Uploads the current spray to the clients. </summary>
    public static void UploadImage2Network()
    {
        var data = CurrentSpray;

        Log.Debug("Uploading new spray");
        // initialize a stream
        Networking.Send(PacketType.ImageChunk, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Int(data.Length);
        }, size: 12);

        Log.Debug("Sending spray data");
        // send data over the stream
        for (int i = 0; i < data.Length; i += ImagePacketSize) Networking.Send(PacketType.ImageChunk, w =>
        {
            Log.Info($"Sending chunk {i}/{data.Length}");
            w.Id(Networking.LocalPlayer.Id);
            w.Bytes(data, i, Mathf.Min(ImagePacketSize, data.Length - i));
        }, size: ImagePacketSize + 8);
    }

    #endregion

    public static void AssignSprayImage(SteamId owner, byte[] bytes)
    {
        var cachedSpray = CheckForCachedSpray(owner);
        cachedSpray?.AssignData(bytes);
        cachedSpray?.AssignCurrent();
    }

    /// <summary> Creates a client spray and send spray. </summary>
    public static void CreateClientSpray(Vector3 position, Vector3 direction)
    {
        Log.Info("Creating new spray");
        // send spray to the clients
        if (LobbyController.Lobby != null) Networking.Send(PacketType.Spray, w =>
        {
            w.Id(Networking.LocalPlayer.Id);
            w.Vector(position);
            w.Vector(direction);
        }, size: 32);

        var cachedSpray = CheckForCachedSpray(Networking.LocalPlayer.Id);
        // if no cached spray, upload it
        if (cachedSpray == null && LobbyController.Lobby != null) 
        {
            // spawn a thread to upload the image, because it's slow, and don't want to freeze the game
            var t = new Thread(UploadImage2Network) { IsBackground = true };
            t.Start();
        }
        cachedSpray?.AssignData(CurrentSpray); // assign the current spray, because it's client side

        // if no cached spray we should assign texture after creating the spray
        var spray = CreateSpray(Networking.LocalPlayer.Id, position, direction, cachedSpray == null ? CurrentSpray : null);
    }

    public static CachedSpray CreateSpray(SteamId owner, Vector3 position, Vector3 direction, byte[] imageData = null)
    {
        var cachedSpray = CheckForCachedSpray(owner);
        if (cachedSpray == null) cachedSpray = CacheSpray(owner);
        else
        {
            Log.Debug($"Cached spray already exists for {owner}");
            if (cachedSpray.SprayExists())
                cachedSpray.Spray.Lifetime = 3f; // we don't want to have many sprays in the scene at once
        }

        // create the spray in scene
        var spray = Spray.Spawn(position, direction);
        cachedSpray.Spray = spray;
        if (imageData != null) cachedSpray.AssignData(imageData);
        cachedSpray.AssignCurrent();

        return cachedSpray;
    }

    /// <summary> Caches the spray in the memory. </summary>
    public static CachedSpray CacheSpray(SteamId owner)
    {
        Log.Debug($"Caching spray for {owner}");
        var cachedSpray = new CachedSpray(owner);
        CachedSprays.Add(cachedSpray);
        return cachedSpray;
    }

    /// <summary> Checks if the given owner has a cached spray. Returns CachedSpray if found, so no need to load it. </summary>
    public static CachedSpray CheckForCachedSpray(SteamId owner) => CachedSprays.Find(s => s.Owner == owner);

    /// <summary> Removes all cached sprays. Sprays need to be loaded again. </summary>
    public static void ClearCachedSprays()
    {
        Log.Debug("Clearing cached sprays");
        CachedSprays.Clear();
    }
    /// <summary> Removes the cached spray from player. </summary>
    public static void ClearCachedSpray(SteamId owner) => CachedSprays.RemoveAll(s => s.Owner == owner);
}