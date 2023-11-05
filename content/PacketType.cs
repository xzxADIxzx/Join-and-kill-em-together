namespace Jaket.Content;

/// <summary> All packet types. Will replenish over time. </summary>
public enum PacketType
{
    /// <summary> Data of an entity, usually a player or an enemy. </summary>
    Snapshot,
    /// <summary> Host asks to load a level, the packet contains its name. </summary>
    LevelLoading,
    /// <summary> Hey Client, could you leave the lobby please? The host asks you to leave the lobby because you were kicked... Cheers~ :heart: </summary>
    Kick,

    /// <summary> Owner of the lobby has died, which means you need to destroy all enemies. </summary>
    HostDied,
    /// <summary> Enemy has died, which means you need to kill him locally. </summary>
    EnemyDied,
    /// <summary> Boss has died, which means you need to kill his original enemy identifier. </summary>
    BossDefeated,

    /// <summary> Client ask the creation of a bullet. </summary>
    SpawnBullet,
    /// <summary> Client dealt damage to an entity. </summary>
    DamageEntity,
    /// <summary> Client punched and this needs to be displayed visually on his doll. </summary>
    Punch,
    /// <summary> Client pointed to somewhere. </summary>
    Point,

    /// <summary> Need to open a certain door. Called by the host so that clients don't get stuck in a room. </summary>
    OpenDoor,
    /// <summary> Need to activate a certain object. It can be anything, because there are a lot of different stuff in the game. </summary>
    ActivateObject,
    /// <summary> Any action with the cinema, like starting a video, pausing or rewinding. </summary>
    CinemaAction
}