namespace Jaket.Sprays;

using Steamworks;
using UnityEngine;

using Jaket.UI.Elements;

/// <summary> Cached spray, that contains the spray and the image data and caches it. </summary>
public class CachedSpray
{
    /// <summary> Who is the owner of the spray. </summary>
    public SteamId Owner;
    /// <summary> Current spray texture. To take data from the image use <see cref="ImageData"/>. </summary>
    public Texture2D Texture;
    /// <summary> The spray in the world. </summary>
    public Spray Spray;
    /// <summary> The cached image data bytes. </summary>
    public byte[] ImageData;

    public CachedSpray(SteamId owner) => Owner = owner;

    public void AssignDataAndUpdate(byte[] data)
    {
        AssignData(data);
        AssignCurrent();
    }

    /// <summary> Assigns the image data to the cached spray. </summary>
    public void AssignData(byte[] data)
    {
        ImageData = data;
        Texture = GetTexture(data);
    }

    /// <summary> Assigns the cached spray data to spray in the world. </summary>
    public void AssignCurrent()
    {
        if (Spray != null && Texture != null)
            Spray.AssignImage(Texture);
    }

    /// <summary> Converts the bytes to a texture. </summary>
    public static Texture2D GetTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        if (bytes.Length > 0) texture.LoadImage(bytes);
        return texture;
    }
}