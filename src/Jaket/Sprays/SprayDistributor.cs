namespace Jaket.Sprays;

using Steamworks.Data;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Admin;

/// <summary> Class responsible for distributing spray images between clients. </summary>
public static class SprayDistributor
{
    /// <summary> Size of the payload of image delivering packets. </summary>
    public const int CHUNK_SIZE = 1024;

    /// <summary> List of streams that upload images. </summary>
    private static Stream[] uploads = new Stream[10];
    /// <summary> List of streams that download images. </summary>
    private static Stream[] downloads = new Stream[10];

    /// <summary> Whether the distributor is busy uploading or downloading images. </summary>
    public static bool Busy => uploads.Any(s => !s.Done) || downloads.Any(s => !s.Done);

    /// <summary> Initializes an uploading of an image. </summary>
    public static void Upload(uint owner, SprayImage image, Connection target)
    {
        return;
        Networking.Send(PacketType.Image, 8, w =>
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
        if (Version.DEBUG) Log.Debug($"[SPRY] Uploading a spray owned by {owner}");
    }

    /// <summary> Initializes a downloading of an image. </summary>
    public static void Download(uint owner, int bytesCount)
    {
        return;
        if (bytesCount > SprayImage.MAX_IMAGE_SIZE && LobbyController.IsOwner)
        {
            Administration.Ban(owner);
            Log.Warning($"[ADMIN] {owner} was blocked: enormous spray");
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
        if (Version.DEBUG) Log.Debug($"[SPRY] Downloading a spray owned by {owner}");
    }

    /// <summary> Processes all of the upload streams. </summary>
    public static void ProcessUploads() => uploads.Each(s => !s.Done, s =>
    {
        int bytesCount = s.Chunks.BytesCount;

        Networking.Send(PacketType.Image, bytesCount + 4, w =>
        {
            w.Id(s.Owner);
            w.Bytes(s.Image.Data, s.Chunks.Processed, bytesCount);
        },
        (data, size) => Networking.Send(s.Target, data, size));

        s.Chunks.Processed += bytesCount;

        if (Version.DEBUG) Log.Debug($"[SPRY] Uploaded {s.Chunks.Progress * 100f:0.00}% of a spray owned by {s.Owner}");
    });

    /// <summary> Processes a download stream of the given member. </summary>
    public static void ProcessDownload(uint owner, int bytesCount, Reader r) => downloads.Each(s => !s.Done && s.Owner == owner, s =>
    {
        if (bytesCount != s.Chunks.BytesCount) return; // something went wrong

        r.Bytes(s.Image.Data, s.Chunks.Processed, bytesCount);

        s.Chunks.Processed += bytesCount;

        if (Version.DEBUG) Log.Debug($"[SPRY] Downloaded {s.Chunks.Progress * 100f:0.00}% of a spray owned by {s.Owner}");

        if (s.Done) SprayManager.Remote[s.Owner] = s.Image;
    });

    /// <summary> Stream that is either uploading or downloading an image. </summary>
    public struct Stream(uint owner, SprayImage image)
    {
        /// <summary> Identifier of the image owner. </summary>
        public uint Owner = owner;
        /// <summary> Number of bytes transmitted. </summary>
        public int Transmitted;
        /// <summary> Image that is being streamed. </summary>
        public SprayImage Image = image;
        /// <summary> Endpoint to upload the image to, nullable. </summary>
        public Connection Target;

        /// <summary> Size of the streamed image. </summary>
        public readonly int Size => Image.Data.Length;
        /// <summary> Size of the upcoming chunk. </summary>
        public readonly int Next => Mathf.Min(Pointers.RESERVED - 9, Size - Transmitted);
        /// <summary> Whether the stream is done. </summary>
        public readonly bool Done => Image == null || Transmitted == Size;
    }

    /// <summary> Action that is either uploading or withdrawing an image. </summary>
    public struct Action(uint owner, SprayImage image, Connection target)
    {
        /// <summary> Processes the action. </summary>
        public readonly void Process()
        {
            if (image == null)
            {
                for (int i = 0; i < uploads  .Length; i++)
                {
                    if (owner == default || uploads  [i].Owner == owner || uploads  [i].Target.UserData == owner) uploads  [i] = default;
                }
                for (int i = 0; i < downloads.Length; i++)
                {
                    if (owner == default || downloads[i].Owner == owner || downloads[i].Target.UserData == owner) downloads[i] = default;
                }
            }
            else Upload(owner, image, target);
        }
    }
}
