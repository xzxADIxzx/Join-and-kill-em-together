namespace Jaket.Sprays;

using System.IO;
using UnityEngine;

/// <summary> File containing an image data aka spray. </summary>
public class SprayFile
{
    /// <summary> Max size of the image in bytes. If the image is bigger, it won't be loaded. </summary>
    public const int MAX_IMAGE_SIZE = 128 * 1024;
    /// <summary> List of supported image extensions. </summary>
    public const string SUPPORTED = "png, jpg, jpeg";

    /// <summary> Name of the file and path to it. </summary>
    public readonly string Name, Path;

    private byte[] data;
    public byte[] Data => data ??= File.ReadAllBytes(Path);

    private Sprite sprite;
    public Sprite Sprite => sprite ??= LoadSprite();

    /// <summary> Converts the data to a sprite. </summary>
    public Sprite LoadSprite()
    {
        Texture2D tex = new(2, 2) { filterMode = FilterMode.Point };
        tex.LoadImage(Data);

        return Sprite.Create(tex, new(0f, 0f, tex.width, tex.height), Vector2.zero, 256);
    }

    /// <summary> Whether the image fits the max size. </summary>
    public bool CheckSize() => new FileInfo(Path).Length <= MAX_IMAGE_SIZE;

    /// <summary> Shortens the name of the file. </summary>
    public string ShortName(int max = 16) => Name.Length > max ? Name.Substring(0, max - 3) + "..." : Name;
}
