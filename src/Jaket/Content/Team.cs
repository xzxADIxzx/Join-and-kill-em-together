namespace Jaket.Content;

using UnityEngine;

using Jaket.Net;

using static Jaket.UI.Lib.Pal;

/// <summary> All teams. They are required by versus mechanics. </summary>
public enum Team
{
    Yellow, Red, Green, Blue, Pink
}

/// <summary> Set of different tools for working with teams. </summary>
public static class Teams
{
    /// <summary> List of all of the teams that is used for iterating. </summary>
    public static Team[] All = { Team.Yellow, Team.Red, Team.Green, Team.Blue, Team.Pink };

    /// <summary> Returns the color of the team. </summary>
    public static Color Color(this Team team) => team switch
    {
        Team.Yellow => yellow,
        Team.Red    => red,
        Team.Green  => green,
        Team.Blue   => blue,
        Team.Pink   => pink,
        _           => white
    };

    /// <summary> Whether the team is allied with the local player. </summary>
    public static bool Ally(this Team team) => team == Networking.LocalPlayer.Team || !LobbyConfig.PvPAllowed;
}
