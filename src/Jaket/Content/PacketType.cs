namespace Jaket.Content;

/// <summary> All packet types. Will replenish over time. </summary>
public enum PacketType : byte
{
    /// <summary> Contains data of a scene. </summary>
    Level,
    /// <summary> Contains data of a spray. </summary>
    Image,

    /// <summary> Contains data of an entity. </summary>
    Snapshot,
    /// <summary> Contains data of a hitscan. </summary>
    Hitscan,
    /// <summary> Contains data of a fraction of dealt damage. </summary>
    Damage,
    /// <summary> Contains data of an entity's death and bits. </summary>
    Death,

    /// <summary> A player changed look. </summary>
    Style,
    /// <summary> A player made a sound. </summary>
    Sound,
    /// <summary> A player pointed somewhere. </summary>
    Point,
    /// <summary> A player sprayed something. </summary>
    Spray,

    /// <summary> Any kind of interaction with the inner world. </summary>
    WorldAction,
    /// <summary> Any kind of interaction with the Cyber Grind. </summary>
    CyberAction,
}
