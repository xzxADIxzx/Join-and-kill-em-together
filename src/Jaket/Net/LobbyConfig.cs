namespace Jaket.Net;

using Steamworks;
using System.Collections.Generic;

/// <summary> Class responsible for configuring the lobby. </summary>
public static class LobbyConfig
{
    /// <summary> Multiplayer provider that owns the lobby. </summary>
    public static string Client
    {
        get => Get("client");
        set => Set("client", value ?? "jaket");
    }
    /// <summary> Name of the lobby, obviously. </summary>
    public static string Name
    {
        get => Get("name");
        set => Set("name", value ?? $"{SteamClient.Name}'s lobby");
    }
    /// <summary> Mode of the lobby, gamemodes. </summary>
    public static string Mode
    {
        get => Get("mode");
        set => Set("mode", value ?? "campaign");
    }
    /// <summary> Mission loaded at the moment. </summary>
    public static string Level
    {
        get => Get("level");
        set => Set("level", value switch
        {
            "Intro"          => "How?",
            "Main Menu"      => "How?",
            "Tutorial"       => "Tutorial",
            "Intermission1"  => "Intermission",
            "Intermission2"  => "Intermission",
            "uk_construct"   => "Sandbox",
            "Endless"        => "Cyber Grind",
            "CreditsMuseum2" => "Museum",
            _                => value[6..]
        });
    }
    /// <summary> Whether the slowmo modifier is enabled. </summary>
    public static bool Slowmo
    {
        get => Get("slowmo") == bool.TrueString;
        set => Set("slowmo", value.ToString());
    }
    /// <summary> Whether the hammer modifier is enabled. </summary>
    public static bool Hammer
    {
        get => Get("hammer") == bool.TrueString;
        set => Set("hammer", value.ToString());
    }
    /// <summary> Whether the bleedy modifier is enabled. </summary>
    public static bool Bleedy
    {
        get => Get("bleedy") == bool.TrueString;
        set => Set("bleedy", value.ToString());
    }
    /// <summary> Whether versus/pure chaos is allowed. </summary>
    public static bool PvPAllowed
    {
        get => Get("allow-pvp") == bool.TrueString;
        set => Set("allow-pvp", value.ToString());
    }
    /// <summary> Whether client-side mods are allowed. </summary>
    public static bool ModsAllowed
    {
        get => Get("allow-mods") == bool.TrueString;
        set => Set("allow-mods", value.ToString());
    }
    /// <summary> Whether bosses are to be healed after player death. </summary>
    public static bool HealBosses
    {
        get => Get("heal-bosses") == bool.TrueString;
        set => Set("heal-bosses", value.ToString());
    }
    /// <summary> Fraction of the initial health that is added to bosses for each player. </summary>
    public static int PPP
    {
        get => int.TryParse(Get("ppp"), out int ppp) ? ppp : 0;
        set => Set("ppp", value.ToString());
    }
    /// <summary> List of privileged players, they can use cheats. </summary>
    public static IEnumerable<string> Privileged
    {
        get => Get("privileged").Split(' ');
        set => Set("privileged", string.Join(' ', value));
    }
    /// <summary> List of banned players, accounts to be accurate. </summary>
    public static IEnumerable<string> Banned
    {
        get => Get("banned").Split(' ');
        set => Set("banned", string.Join(' ', value));
    }

    /// <summary> Resets the lobby config to its default value. </summary>
    public static void Reset()
    {
        Client = null;
        Name = null;
        Mode = null;
        Level = Scene;

        PvPAllowed = true;
        ModsAllowed = true;
        HealBosses = true;
        Privileged = [AccId.ToString()];
    }

    /// <summary> Sets lobby data by the given key. </summary>
    public static void Set(string key, string value) => LobbyController.Lobby?.SetData(key, value);

    /// <summary> Gets lobby data by the given key. </summary>
    public static string Get(string key) => LobbyController.Lobby?.GetData(key);
}
