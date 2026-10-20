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
    /// <summary> List of streams for uploading images. </summary>
    private static Stream[] uploads = new Stream[16];
    /// <summary> List of streams for downloading images. </summary>
    private static Stream[] downloads = new Stream[7];

    /// <summary> Initializes an uploading of an image. </summary>
    private static void Upload(uint owner, SprayImage image, Connection target)
    {
        Networking.Send(PacketType.Image, 8, w =>
        {
            w.Id(owner);
            w.Int(image.Data.Length);
        },
        (data, size) => Networking.Send(target, data, size));

        for (int i = 0; i < uploads.Length; i++)
        {
            if (uploads[i].Done)
            {
                uploads[i] = new(owner, image) { Target = target };
                break;
            }
            if (uploads.Length - 1 == i)
            {
                Log.Error($"[SPRY] Couldn't find an available stream for uploading an image owned by {owner}");
                return;
            }
        }
        if (Version.DEBUG) Log.Debug($"[SPRY] Uploading an image owned by {owner}");
    }

    /// <summary> Initializes a downloading of an image. </summary>
    private static void Download(uint owner, int bytesCount)
    {
        if (bytesCount > SprayImage.MAX_IMAGE_SIZE && LobbyController.IsOwner)
        {
            Administration.Ban(owner);
            Log.Warning($"[ADMIN] {owner} was blocked: enormous image");
        }
        else if (bytesCount > SprayImage.MAX_IMAGE_SIZE) return;

        for (int i = 0; i < downloads.Length; i++)
        {
            if (downloads[i].Done)
            {
                downloads[i] = new(owner, new(new byte[bytesCount]));
                break;
            }
            if (downloads.Length - 1 == i)
            {
                Log.Error($"[SPRY] Couldn't find an available stream for downloading an image owned by {owner}");
                return;
            }
        }
        if (Version.DEBUG) Log.Debug($"[SPRY] Downloading an image owned by {owner}");
    }

    /// <summary> Processes all of the upload streams. </summary>
    public static void ProcessUploads() => Stats.Measure(ref Stats.Common, () =>
    {
        if (LobbyController.Offline) return;

        while (SprayManager.Bridge.TryDequeue(out var upload)) upload.Process();

        for (int i = 0; i < uploads.Length; i++)
        {
            var s = uploads[i];
            if (s.Done) continue;

            Networking.Send(PacketType.Image, 8 + s.Next, w =>
            {
                w.Id(s.Owner);
                w.Int(s.Next);
                w.Bytes(s.Image.Data, s.Transmitted, s.Next);
            },
            (data, size) => Networking.Send(s.Target, data, size));

            uploads[i].Transmitted += s.Next;
            s = uploads[i];

            if (Version.DEBUG) Log.Debug($"[SPRY] Uploaded {100f * s.Transmitted / s.Size:0.00}% of an image owned by {s.Owner}");
        }
    });

    /// <summary> Processes one of the download streams. </summary>
    public static void ProcessDownload(uint owner, int bytesCount, int actual, Reader r) => Stats.Measure(ref Stats.Common, () =>
    {
        if (actual == 0)
        {
            for (int i = 0; i < downloads.Length; i++) if (downloads[i].Owner == owner) downloads[i] = default;
            Download(owner, bytesCount);
        }
        else for (int i = 0; i < downloads.Length; i++)
        {
            var s = downloads[i];
            if (s.Done || s.Owner != owner) continue;

            if (bytesCount != s.Next || bytesCount != actual) return; // invalid data

            r.Bytes(s.Image.Data, s.Transmitted, s.Next);

            downloads[i].Transmitted += s.Next;
            s = downloads[i];

            if (Version.DEBUG) Log.Debug($"[SPRY] Downloaded {100f * s.Transmitted / s.Size:0.00}% of an image owned by {s.Owner}");

            if (s.Done) Events.Post(() => SprayManager.Remote[s.Owner] = s.Image);
            return;
        }
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
