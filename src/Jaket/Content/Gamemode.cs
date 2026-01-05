namespace Jaket.Content;

/// <summary> All gamemodes. Will replenish over time. </summary>
public enum Gamemode : byte
{
    Campaign,
    Manhunt,
    Versus,
    Arena,
    ArmsRace,
    PaintTheWorld,
    HideAndSeek,
}

/// <summary> Set of different tools for working with gamemodes. </summary>
public static class Gamemodes
{
    /// <summary> List of all of the gamemodes that is used for iterating. </summary>
    public static Gamemode[] All = { Gamemode.Campaign, Gamemode.Manhunt, Gamemode.Versus, Gamemode.Arena, Gamemode.ArmsRace, Gamemode.PaintTheWorld, Gamemode.HideAndSeek };

    /// <summary> Whether the gamemode implies a deadly competition between all teams. </summary>
    public static bool PvP(this Gamemode gm) => gm switch
    {
        Gamemode.Versus        => true,
        Gamemode.Arena         => true,
        Gamemode.ArmsRace      => true,
        Gamemode.HideAndSeek   => true,
        _                      => false,
    };

    /// <summary> Whether the gamemode implies team based health points. </summary>
    public static bool HPs(this Gamemode gm) => gm switch
    {
        Gamemode.Versus        => true,
        Gamemode.Arena         => true,
        _                      => false,
    };

    /// <summary> Whether the gamemode implies healing after killing a player. </summary>
    public static bool HealOnKill(this Gamemode gm) => gm switch
    {
        Gamemode.Versus        => true,
        Gamemode.HideAndSeek   => true,
        _                      => false,
    };

    /// <summary> Whether the gamemode implies the absence of common enemies. </summary>
    public static bool NoCommonEnemies(this Gamemode gm) => gm switch
    {
        Gamemode.Versus        => true,
        Gamemode.Arena         => true,
        Gamemode.ArmsRace      => true,
        Gamemode.PaintTheWorld => true,
        Gamemode.HideAndSeek   => true,
        _                      => false,
    };

    /// <summary> Whether the gamemode implies the presence of enemies waves. </summary>
    public static bool WaveLikeEnemies(this Gamemode gm) => gm switch
    {
        Gamemode.Arena         => true,
        _                      => false,
    };

    /// <summary> Whether the gamemode implies the absence of restart button. </summary>
    public static bool NoRestarts(this Gamemode gm) => gm switch
    {
        Gamemode.Versus        => true,
        Gamemode.Arena         => true,
        Gamemode.ArmsRace      => true,
        Gamemode.HideAndSeek   => true,
        _                      => false,
    };
}
