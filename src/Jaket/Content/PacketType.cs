namespace Jaket.Content;

/// <summary> All packet types. Will replenish over time. </summary>
public enum PacketType : byte
{
    /// <summary> Initiates loading of the level selected by the lobby owner. </summary>
    Level,

    /// <summary> Contains data of an entity. </summary>
    Snapshot,
    /// <summary> Contains data of a hitscan. </summary>
    Hitscan,
    /// <summary> Contains data of a fraction of dealt damage. </summary>
    Damage,
    /// <summary> Contains data of an entity's death and bits. </summary>
    Death,

    /// <summary> A player changed their look. </summary>
    Style,
    /// <summary> A player punched or parried. </summary>
    Punch,
    /// <summary> A player pointed somewhere. </summary>
    Point,
    /// <summary> A player sprayed something. </summary>
    Spray,

    /// <summary> Initiates loading of the image selected by a player. </summary>
    ImageHeader,
    /// <summary> Contains a chunk of the image data to be delivered. </summary>
    ImageChunk,

    /// <summary> Any kind of interaction with the inner world. </summary>
    WorldAction,
    /// <summary> Any kind of interaction with the Cyber Grind. </summary>
    CyberAction,
}
