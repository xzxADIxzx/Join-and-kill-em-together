namespace Jaket.Sprays;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Dialogs;

/// <summary> Class responsible for managing spray images. </summary>
public static class SprayManager
{
    /// <summary> Images of the local player. </summary>
    public static List<SprayImage> Local = new();
    /// <summary> Images of the remote players. </summary>
    public static Dictionary<uint, SprayImage> Remote = new();

    /// <summary> Image chosen by the local player. </summary>
    public static SprayImage Selected, Uploaded;
    /// <summary> Sound played by new spray elements. </summary>
    public static AudioClip Puh;

    /// <summary> Loads spray images of the local player. </summary>
    public static void Load()
    {
        Local.Clear();

        Files.MakeDir(Files.Sprays);
        Files.IterAll(f => Local.Add(new(f)), Files.Sprays, SprayImage.SUPPORTED.Split('#'));

        Log.Info($"[SPRY] Loaded {Local.Count} sprays: {string.Join(", ", Local.ConvertAll(s => s.Name))}");

        // this method may be called multiple times to refresh the local images
        if (Puh) return;

        GameAssets.Sound("Impacts/Explosion Harmless.wav", c => Puh = c);
        SpraySettings.Load();

        Events.OnLobbyEnter += () => Uploaded = null;
        Events.OnLoad += () =>
        {
            if (!LobbyController.IsOwner && Uploaded == null && Selected != null) UploadLocal();
        };
        Events.OnMemberJoin += m =>
        {
            if (LobbyController.IsOwner) Upload(m.Id.AccountId);
        };

        Events.OnLobbyEnter += () =>
        {
            Remote.Clear();
            SprayDistributor.Remove();
        };
        Events.OnMemberLeave += m =>
        {
            Remote.Remove(m.Id.AccountId);
            SprayDistributor.Remove(m.Id.AccountId);
        };
        Events.EveryTick += SprayDistributor.ProcessUploads;
    }

    /// <summary> Finds spray image of the given member. </summary>
    public static SprayImage Find(uint owner)
    {
        if (owner == AccId && Selected == null)
        {
            Bundle.Hud("spray.choose");
            return null;
        }
        if (Administration.Hidden.Contains(owner) || !SpraySettings.Enabled) return null;

        return owner == AccId ? Selected : Remote.TryGetValue(owner, out var s) ? s : null;
    }

    /// <summary> Uploads all sprays to the given member. </summary>
    public static void Upload(uint target)
    {
        if (SprayDistributor.Busy || Networking.Connections.All(c => c.ConnectionName != target.ToString()))
        {
            Events.Post2(() => Upload(target));
            return;
        }
        var con = Networking.Connections.Find(c => c.ConnectionName == target.ToString());

        if (Selected != null) SprayDistributor.Upload(AccId, Selected, con);

        Remote.Each(p => SprayDistributor.Upload(p.Key, p.Value, con));
    }

    /// <summary> Uploads the selected spray to all members. </summary>
    public static void UploadLocal()
    {
        if (SprayDistributor.Busy)
        {
            Events.Post2(UploadLocal);
            return;
        }
        if (LobbyController.IsOwner)
            Networking.Connections.Each(c => SprayDistributor.Upload(AccId, Uploaded = Selected, c));
        else
            SprayDistributor.Upload(AccId, Uploaded = Selected, Networking.Client.Manager.Connection);
    }
}
