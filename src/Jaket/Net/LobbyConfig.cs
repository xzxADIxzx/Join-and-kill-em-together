namespace Jaket.Net;

using Steamworks;

/// <summary> Class responsible for configuring the lobby. </summary>
public static class LobbyConfig
{
    /// <summary> Multiplayer provider that owns the lobby. </summary>
    public static string Client
    {
        get => Get("client");
        set => Set("client", value);
    }
    /// <summary> Name of the lobby, obviously. </summary>
    public static string Name
    {
        get => Get("name");
        set => Set("name", value ?? $"{SteamClient.Name}'s lobby");
    }
    /// <summary> Mission that is loaded in the lobby. </summary>
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
            _ => value[6..]
        });
    }

    /// <summary> Sets lobby data by the given key to the given value. </summary>
    public static void Set(string key, string value) => LobbyController.Lobby?.SetData(key, value);

    /// <summary> Gets lobby data by the given key. </summary>
    public static string Get(string key) => LobbyController.Lobby?.GetData(key);
}
