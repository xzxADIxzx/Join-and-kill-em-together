namespace Jaket.Content;

using System;
using UnityEngine;

/// <summary> All teams. Teams needed for PvP mechanics. </summary>
public enum Team
{
    [TeamData(1f, .827451f, .49803922f)]
    Yellow,

    [TeamData(1f, 0f, 0f)]
    Red,

    [TeamData(0f, 1f, 0f)]
    Green,

    [TeamData(0f, 0f, 1f)]
    Blue,

    [TeamData(1f, .4117647f, .7058824f, true)]
    Pink
}

/// <summary> Attribute containing team data. </summary>
[AttributeUsage(AttributeTargets.Field)]
public class TeamData : Attribute
{
    /// <summary> Team color. Only used in interface. </summary>
    private float r, g, b;
    /// <summary> Whether the wings should be pink. </summary>
    private bool pink;

    public TeamData(float r, float g, float b, bool pink = false)
    {
        this.r = r;
        this.b = b;
        this.g = g;
        this.pink = pink;
    }

    /// <summary> Returns the team color. </summary>
    public Color Color() => new Color(r, g, b);

    /// <summary> Returns the color of the wings. </summary>
    public Color WingColor() => pink ? new Color(2f, 1f, 12f) : UnityEngine.Color.white;
}

/// <summary> Extension class that allows you to get team data. </summary>
public static class Extension
{
    public static TeamData Data(this Team team)
    {
        string name = Enum.GetName(typeof(Team), team);
        return Attribute.GetCustomAttribute(typeof(Team).GetField(name), typeof(TeamData)) as TeamData;
    }
}
