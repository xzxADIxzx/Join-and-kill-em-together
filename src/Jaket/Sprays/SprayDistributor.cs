namespace Jaket.Sprays;

using Steamworks.Data;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class responsible for distributing spray images between clients. </summary>
public static class SprayDistributor
{
    /// <summary> Size of the payload of image delivering packets. </summary>
    public const int CHUNK_SIZE = 1024;

    /// <summary> List of streams that upload images. </summary>
    private static Stream[] uploads = new Stream[16];
    /// <summary> List of streams that download images. </summary>
    private static Stream[] downloads = new Stream[16];

    /// <summary> Whether the distributor is busy uploading or downloading images. </summary>
    public static bool Busy => uploads.Any(s => !s.Done) || downloads.Any(s => !s.Done);

    /// <summary> Initializes an uploading of an image. </summary>
    public static void Upload(uint owner, SprayImage image, Connection target)
    {
        Networking.Send(PacketType.ImageHeader, 8, w =>
        {
            w.Id(owner);
            w.Int(image.Data.Length);
        },
        (data, size) => Networking.Send(target, data, size));

        for (int i = 0; i < uploads.Length; i++)
        {
            if (uploads[i].Owner == owner || uploads[i].Done)
            {
                uploads[i] = default(Stream) with { Owner = owner, Image = image, Target = target, Chunks = new(image.Data.Length) };
                break;
            }
        }
        if (Version.DEBUG) Log.Debug($"[SPRY] Uploading a spray owner by {owner}");
    }

    /// <summary> Initializes a downloading of an image. </summary>
    public static void Download(uint owner, int bytesCount)
    {
        if (bytesCount > SprayImage.MAX_IMAGE_SIZE && LobbyController.IsOwner)
        {
            Administration.Ban(owner);
            Log.Warning($"[SPRY] {owner} was blocked: enormous spray");
        }
        else if (bytesCount > SprayImage.MAX_IMAGE_SIZE) return;

        for (int i = 0; i < downloads.Length; i++)
        {
            if (downloads[i].Owner == owner || downloads[i].Done)
            {
                downloads[i] = default(Stream) with { Owner = owner, Image = new(new byte[bytesCount]), Chunks = new(bytesCount) };
                break;
            }
        }
        if (Version.DEBUG) Log.Debug($"[SPRY] Downloading a spray owner by {owner}");
    }

    /// <summary> Processes all of the upload streams. </summary>
    public static void ProcessUploads() => uploads.Each(s => !s.Done, s =>
    {
        int bytesCount = s.Chunks.BytesCount;

        Networking.Send(PacketType.ImageChunk, bytesCount + 4, w =>
        {
            w.Id(s.Owner);
            w.Bytes(s.Image.Data, s.Chunks.Processed, bytesCount);
        },
        (data, size) => Networking.Send(s.Target, data, size));

        s.Chunks.Processed += bytesCount;

        if (Version.DEBUG) Log.Debug($"[SPRY] Uploaded {s.Chunks.Progress * 100f:0.00}% of a spray owner by {s.Owner}");
    });

    /// <summary> Processes a download stream of the given member. </summary>
    public static void ProcessDownload(uint owner, int bytesCount, Reader r) => downloads.Each(s => !s.Done && s.Owner == owner, s =>
    {
        if (bytesCount != s.Chunks.BytesCount) return; // something went wrong

        r.Bytes(s.Image.Data, s.Chunks.Processed, bytesCount);

        s.Chunks.Processed += bytesCount;

        if (Version.DEBUG) Log.Debug($"[SPRY] Downloaded {s.Chunks.Progress * 100f:0.00}% of a spray owner by {s.Owner}");

        if (s.Done) SprayManager.Remote[s.Owner] = s.Image;
    });

    /// <summary> Removes all of the streams of the given member. </summary>
    public static void Remove(uint? owner = null)
    {
        for (int i = 0; i < uploads.Length; i++)
        {
            if (owner == null || uploads[i].Owner == owner) uploads[i] = default;
        }
        for (int i = 0; i < downloads.Length; i++)
        {
            if (owner == null || downloads[i].Owner == owner) downloads[i] = default;
        }
    }

    /// <summary> Stream that is either uploading or downloading an image. </summary>
    public struct Stream
    {
        /// <summary> Identifier of the player who owns the spray image. </summary>
        public uint Owner;
        /// <summary> Image itself that is being streamed over the network. </summary>
        public SprayImage Image;
        /// <summary> Table counting proccessed chunks of the spray image. </summary>
        public ChunkTable Chunks;
        /// <summary> Endpoint to upload the spray image to, may be null. </summary>
        public Connection Target;

        /// <summary> Whether the streaming process is completed or interrupted. </summary>
        public readonly bool Done => Chunks == null || Chunks.Processed == Chunks.ImageSize;
    }

    /// <summary> Table that controls an uploading or downloading process. </summary>
    public class ChunkTable
    {
        /// <summary> Size of the image and the amount of processed bytes. </summary>
        public int ImageSize, Processed;
        /// <summary> Size of the chunk to be processed on the next subtick. </summary>
        public int BytesCount => Mathf.Min(CHUNK_SIZE, ImageSize - Processed);
        /// <summary> Ratio of processed bytes to the image size. </summary>
        public float Progress => (float)Processed / ImageSize;

        public ChunkTable(int size) => ImageSize = size;
    }
}
