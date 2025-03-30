namespace Jaket.Sprays;

using UnityEngine;

using Jaket.IO;

/// <summary> Represents a spray stored in either memory or a local file. </summary>
public class SprayFile
{
    /// <summary> Max size of the image in bytes. If the image is bigger, it will not be loaded. </summary>
    public const int MAX_IMAGE_SIZE = 512 * 1024;
    /// <summary> List of supported image extensions. </summary>
    public const string SUPPORTED = "*.png#*.jpg#*.jpeg";

    /// <summary> Name of the file and its path. </summary>
    public readonly string Name, Path;

    /// <summary> Short version of the file name. </summary>
    public string Short => Name.Length < 22 ? Name : $"{Name[..18]}...";
    /// <summary> Whether the image fits the max size. </summary>
    public bool Valid => Files.Exists(Path) && Files.Size(Path) < MAX_IMAGE_SIZE;

    private byte[] data;
    public byte[] Data => data ??= Files.ReadBytes(Path);

    private Sprite sprite;
    public Sprite Sprite => sprite ??= LoadSprite(Data);

    /// <summary> Converts the given data into a sprite. </summary>
    public static Sprite LoadSprite(byte[] data)
    {
        Texture2D tex = new(2, 2) { filterMode = FilterMode.Point };
        tex.LoadImage(data);
        return Sprite.Create(tex, new(0f, 0f, tex.width, tex.height), Vector2.zero, 256f);
    }

    public SprayFile(string path)
    {
        Name = Files.GetName(path);
        Path = path;
    }

    public SprayFile(byte[] data)
    {
        Name = Path = "Net";
        this.data = data;
    }
}
