namespace Jaket.Sprays;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Dialogs;

/// <summary> Class responsible for distributing sprays between clients. </summary>
public static class SprayDistributor
{
    /// <summary> Size of the packet that contains an image chunk. </summary>
    public const int CHUNK_SIZE = 240;

    /// <summary> List of all streams for spray loading. </summary>
    public static Dictionary<ulong, Writer> Streams = new();
    /// <summary> List of requests for spray by id. </summary>
    public static Dictionary<ulong, List<Connection>> Requests = new();

    #region distribution logic

    /// <summary> Processes all spray requests. </summary>
    public static void ProcessRequests()
    {
        foreach (var owner in Requests.Keys)
        {
            if (SprayManager.Cache.TryGetValue(owner, out var spray))
                Upload(owner, spray.Data, (data, size) => Requests[owner].ForEach(con => Tools.Send(con, data, size)));
            else
                Log.Error($"Couldn't find the requested spray. Spray id is {owner}");
        }

        Requests.Clear(); // clear all requests, because they are processed
    }

    /// <summary> Handles the downloaded spray and decides where to send it next. </summary>
    public static void HandleSpray(ulong owner, byte[] data)
    {
        SprayManager.Cache.Remove(owner);
        SprayManager.Cache.Add(owner, new(data));

        // update the existing spray if there is one
        if (SprayManager.Sprays.TryGetValue(owner, out var spray)) spray.UpdateSprite();
    }

    /// <summary> Requests someone's spray from the host. </summary>
    public static void Request(SteamId owner) => Networking.Send(PacketType.RequestImage, w => w.Id(owner), size: 8);

    #endregion
    #region networking

    /// <summary> Uploads the given spray to the clients or server. </summary>
    public static void Upload(SteamId owner, byte[] data, Action<IntPtr, int> result = null)
    {
        // initialize a new stream
        Networking.Send(PacketType.ImageChunk, w =>
        {
            w.Id(owner);
            w.Int(data.Length);
        }, result, 12);

        // send data over the stream
        for (int i = 0; i < data.Length; i += CHUNK_SIZE) Networking.Send(PacketType.ImageChunk, w =>
        {
            w.Id(owner);
            w.Bytes(data, i, Mathf.Min(CHUNK_SIZE, data.Length - i));
        }, result, CHUNK_SIZE + 8);
    }

    /// <summary> Uploads the current spray to the server. </summary>
    public static void UploadLocal()
    {
        // there is no point in sending the spray to the distributor if you haven't changed it
        if (SprayManager.Uploaded || SprayManager.CurrentSpray == null) return;
        Log.Info("Uploading the current spray...");

        Upload(Networking.LocalPlayer.Id, SprayManager.CurrentSpray.Data);
        SprayManager.Uploaded = true;
    }

    /// <summary> Loads a spray from the client or server. </summary>
    public static void Download(Reader r)
    {
        if (!SpraySettings.Enabled) return;

        var id = r.Id(); // id of the spray owner
        if (Streams.TryGetValue(id, out var stream)) // data packet
        {
            stream.Bytes(r.Bytes(Mathf.Min(CHUNK_SIZE, r.Length - 9)));
            if (stream.Position >= stream.Length)
            {
                // handle the downloaded spray
                Reader.Read(stream.mem, stream.Length, r => HandleSpray(id, r.Bytes(r.Length)));

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
            Log.Info("Downloading spray#" + id);

            int length = r.Int();
            Streams.Add(id, new(Marshal.AllocHGlobal(length), length));
        }
    }

    #endregion
}
