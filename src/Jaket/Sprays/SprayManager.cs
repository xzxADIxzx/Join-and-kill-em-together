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
            if (Uploaded == null && Selected != null && !LobbyController.IsOwner)
            {
                Uploaded = Selected;
                SprayDistributor.Upload(AccId, Selected, Networking.Client.Manager.Connection);
            }
        };
        Events.OnMemberJoin += m =>
        {
            if (LobbyController.IsOwner) ; // TODO add member to the upload queue
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
            Bundle.Hud("sprays.nospray");
            return null;
        }
        if (Administration.Hidden.Contains(owner) || !SpraySettings.Enabled) return null;

        return owner == AccId ? Selected : Remote.TryGetValue(owner, out var s) ? s : null;
    }
}
