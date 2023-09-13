namespace Jaket.World;

using System;
using UnityEngine.Networking;
using UnityEngine.Video;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI;

/// <summary> Class responsible for loading and playing the video on the cinema screen. </summary>
public class Cinema
{
    /// <summary> YouTube URL to download videos from. </summary>
    public const string YOUTUBE_URL = "https://www.youtube.com/watch?v=";
    /// <summary> Json fragment that precedes a link to the mp4 video file. </summary>
    public const string FORMATS = "\"formats\":[{\"itag\":18,\"url\":\"";

    /// <summary> Returns the cinema video player component. </summary>
    public static VideoPlayer Player() => UnityEngine.Object.FindObjectOfType<VideoPlayer>();

    /// <summary> Starts a video received from the given link. </summary>
    public static void Play(string url) => Player().url = url;

    /// <summary> Plays YouTube video with given id. </summary>
    public static void PlayById(string videoId) => Fetch(videoId, (done, result) =>
    {
        if (done && Parse(result, out var url))
        {
            Play(url); // play the video and send the link to it to all clients
            Networking.Redirect(Writer.Write(w => w.String(url)), PacketType.CinemaAction);

            // turn on the video player
            CinemaPlayer.Instance.Play();
        }
    });

    /// <summary> Fetches YouTube video website with given id. </summary>
    public static void Fetch(string videoId, Action<bool, string> result)
    {
        var request = UnityWebRequest.Get(YOUTUBE_URL + videoId);
        request.SendWebRequest().completed += _ => result(request.isDone, request.downloadHandler.text);
    }

    /// <summary> Extracts a link to an mp4 file from a YouTube vide website. </summary>
    public static bool Parse(string result, out string url)
    {
        // default value with sad emoji
        url = "Failed to parse data ;(";

        int index = result.IndexOf(FORMATS);
        if (index == -1) return false;

        var clipped = result.Substring(index + FORMATS.Length, 5000);
        url = clipped.Substring(0, clipped.IndexOf('"')).Replace("\\u0026", "&");

        return url.StartsWith("https://");
    }
}
