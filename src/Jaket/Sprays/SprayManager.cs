namespace Jaket.Sprays;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Dialogs;
using Jaket.UI.Elements;
using Jaket.IO;

/// <summary> Saves sprays of players and loads sprays of the local player. </summary>
public class SprayManager
{
    /// <summary> Spray currently selected by the local player. </summary>
    public static SprayFile CurrentSpray;
    /// <summary> Other sprays located in the sprays folder. </summary>
    public static List<SprayFile> Loaded = new();
    /// <summary> Whether the current spray has been uploaded. </summary>
    public static bool Uploaded;

    /// <summary> Sprays spawned by players. </summary>
    public static Dictionary<uint, Spray> Sprays = new();
    /// <summary> Cached sprays of other players. </summary>
    public static Dictionary<uint, SprayFile> Cache = new();

    /// <summary> Sound that is played when creating a spray. </summary>
    public static AudioClip puh;

    /// <summary> Subscribes to various events to synchronize sprays or clear cache. </summary>
    public static void Load()
    {
        LoadSprayFiles();
        SpraySettings.Load();

        ResFind<AudioClip>().Each(clip => clip.name == "Explosion Harmless", clip => puh = clip);

        // clear the cache in offline game & upload the current spray if it was changed
        Events.OnLoad += () =>
        {
            if (LobbyController.Offline) Cache.Clear();
            else SprayDistributor.UploadLocal();

            foreach (var spray in Sprays.Values)
                if (spray != null) spray.Lifetime = 60f;
        };
        Events.OnLobbyEnter += () =>
        {
            Uploaded = LobbyController.IsOwner;
            Cache.Clear();
            Cache.Add(AccId, CurrentSpray);
        };
        Events.EveryHalf += SprayDistributor.ProcessRequests;
    }

    /// <summary> Loads sprays from the sprays folder. </summary>
    public static void LoadSprayFiles()
    {
        Loaded.Clear();

        Files.MakeDir(Files.Sprays);
        Files.IterAll(f => Loaded.Add(new(f)), Files.Sprays, SprayFile.SUPPORTED.Split('#'));

        Log.Info($"Loaded {Loaded.Count} sprays: {string.Join(", ", Loaded.ConvertAll(s => s.Name))}");
    }

    /// <summary> Sets the current spray. </summary>
    public static void SetSpray(SprayFile spray)
    {
        CurrentSpray = spray;
        Uploaded = false;

        Cache.Remove(AccId);
        Cache.Add(AccId, CurrentSpray);
    }

    /// <summary> Spawns someone's spray in the given position. </summary>
    public static Spray Spawn(uint owner, Vector3 position, Vector3 direction)
    {
        if (Sprays.TryGetValue(owner, out var spray))
        {
            spray.Lifetime = 58f;
            Sprays.Remove(owner);
        }
        if (!Cache.ContainsKey(owner))
        {
            if (owner == AccId) // seems like the player is in offline game
                Cache.Add(owner, CurrentSpray);
            else
                SprayDistributor.Request(owner);
        }

        spray = Spray.Spawn(owner, position, direction);
        Sprays.Add(owner, spray);

        return spray;
    }

    /// <summary> Spawns the local player's spray in the given position. </summary>
    public static Spray Spawn(Vector3 position, Vector3 direction)
    {
        if (CurrentSpray == null)
        {
            Bundle.Hud("sprays.empty"); // You haven't chosen a spray. Please, choose on in settings.
            return null;
        }
        return Spawn(AccId, position, direction);
    }
}
