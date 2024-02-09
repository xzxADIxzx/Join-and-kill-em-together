namespace Jaket.Sprays;

using System.IO;
using Steamworks;
using UnityEngine;

/// <summary> File that contains the spray and the image data. </summary>
public class SprayFile
{
    public const int DefaultShortNameLength = 8;
    public const int ImageMaxSize = 512 * 1024;

    public string Name;
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
    public string GetShortName(int length = DefaultShortNameLength) => Name.Length > length ? Name.Substring(0, length) + "..." : Name;

    /// <summary> Converts the bytes to a texture. </summary>
    public static Texture2D GetTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        if (bytes.Length > 0) texture.LoadImage(bytes);
        return texture;
    }
    
    /// <summary> Returns true if the image is too big. </summary>
    public bool CheckSize() => ImageData.Length > ImageMaxSize;
}