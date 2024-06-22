namespace Jaket.Content;

/// <summary> All packet types. Will replenish over time. </summary>
public enum PacketType
{
    /// <summary> Data of an entity: player, enemy, item and etc. </summary>
    Snapshot,
    /// <summary> Initializes a loading of the level requested by the host. </summary>
    Level,
    /// <summary> Hey Client, could you leave the lobby please? The host asks you to leave the lobby because you were banned... Cheers~ :heart: </summary>
    Ban,

    /// <summary> Data of the bullet spawned by a player. </summary>
    SpawnBullet,
    /// <summary> Data of the damage dealt to an entity. </summary>
    DamageEntity,
    /// <summary> Request from a player to kill an entity or to destroy a bullet. </summary>
    KillEntity,

    /// <summary> Player changed their style: the color of weapons or clothes. </summary>
    Style,
    /// <summary> Player punched, this needs to be visually displayed. </summary>
    Punch,
    /// <summary> Player pointed to some point in space. </summary>
    Point,

    /// <summary> Player sprayed something. </summary>
    Spray,
    /// <summary> Image chunk from the sprayer. </summary>
    ImageChunk,
    /// <summary> Player asked the host to give him someone's spray data. </summary>
    RequestImage,

    /// <summary> Need to activate a certain object. It can be anything, because there are a lot of different stuff in the game. </summary>
    ActivateObject,
    /// <summary> Any action with CyberGrind, like pattern and wave. </summary>
    CyberGrindAction
}
