namespace Jaket.Content;

/// <summary> All packet types. Will replenish over time. </summary>
public enum PacketType : byte
{
    /// <summary> Initiates loading of the level selected by the lobby owner. </summary>
    Level,
    /// <summary> Dear client, could you leave the lobby please? Cheers~ :heart: </summary>
    Ban,

    /// <summary> Contains data of a particular entity. </summary>
    Snapshot,
    /// <summary> Contains data of a hitscan shot. </summary>
    Hitscan,

    /// <summary> Contains data of a dealt damage unit. </summary>
    Damage,
    /// <summary> Contains data of a death and some entity-specific flags. </summary>
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

    /// <summary> Player voted for an option. Voting can be different: skip of a cutscene, dialog or choice of an option. </summary>
    Vote
}
