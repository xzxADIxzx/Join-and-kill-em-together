namespace Jaket.Sprays;

using UnityEngine;

using Jaket.IO;

/// <summary> Represents a titled image stored in memory. </summary>
public class SprayImage
{
    /// <summary> If an image is bigger, it won't be loaded to memory. </summary>
    public const int MAX_IMAGE_SIZE = 512 * 1024;
    /// <summary> List of supported image extensions. </summary>
    public const string SUPPORTED = "*.png#*.jpg#*.jpeg";

    /// <summary> Name of the image and its path. </summary>
    public readonly string Name, Path;

    /// <summary> Short version of the file name. </summary>
    public string Short => Name.Length < 27 ? Name : $"{Name[..23]}...";
    /// <summary> Whether the image fits the maximum size. </summary>
    public bool Valid => Files.Exists(Path) && Files.Size(Path) <= MAX_IMAGE_SIZE;

    private byte[] data;
    public byte[] Data => data ??= Files.ReadBytes(Path);

    private Sprite sprite;
    public Sprite Sprite => sprite ??= MakeSprite(Data);

    /// <summary> Creates a sprite from the given data. </summary>
    public static Sprite MakeSprite(byte[] data)
    {
        Texture2D tex = new(2, 2) { filterMode = FilterMode.Point };
        tex.LoadImage(data);
        return Sprite.Create(tex, new(0f, 0f, tex.width, tex.height), Vector2.zero, 256f);
    }

    public SprayImage(string path)
    {
        Name = Files.Name(path);
        Path = path;
    }

    public SprayImage(byte[] data)
    {
        Name = Path = "NETWORK";
        this.data = data;
    }
}
