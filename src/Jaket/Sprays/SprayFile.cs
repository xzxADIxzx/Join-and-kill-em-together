namespace Jaket.Sprays;

using System.IO;

using UnityEngine;

/// <summary> File that contains the spray and the image data. </summary>
public class SprayFile
{
    public const int DEFAULT_SHORT_NAME_LENGTH = 8;
    /// <summary> The maximum size of the image in bytes. If the image is bigger, it won't be loaded. </summary>
    public const int IMAGE_MAX_SIZE = 512 * 1024;

    public string Name;
    /// <summary> Loaded image data bytes. </summary>
    public byte[] ImageData;
    public Texture2D Texture;

    public SprayFile(string name, string path)
    {
        Name = name;

        // Load image and add the texture
        ImageData = File.ReadAllBytes(path);
        Texture = GetTexture(ImageData);
    }

    /// <summary> Shortens the name of the file to a maximum of 8 characters. </summary>
    public string GetShortName(int length = DEFAULT_SHORT_NAME_LENGTH) => Name.Length > length ? Name.Substring(0, length) + "..." : Name;

    /// <summary> Converts the bytes to a texture. </summary>
    public static Texture2D GetTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        if (bytes.Length > 0) texture.LoadImage(bytes);
        return texture;
    }
    
    /// <summary> Returns true if the image is too big. </summary>
    public bool CheckSize() => ImageData.Length > IMAGE_MAX_SIZE;
}