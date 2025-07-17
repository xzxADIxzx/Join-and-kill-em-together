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

    /// <summary> Image chunk from the sprayer. </summary>
    ImageChunk,
    /// <summary> Player asked the host to give him someone's spray data. </summary>
    RequestImage,

    /// <summary> Need to activate a certain object. It can be anything, because there are a lot of different stuff in the game. </summary>
    ActivateObject,
    /// <summary> Any action with CyberGrind, like pattern and wave. </summary>
    CyberGrindAction,
    /// <summary> Player voted for an option. Voting can be different: skip of a cutscene, dialog or choice of an option. </summary>
    Vote
}
