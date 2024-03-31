namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System;
using System.Linq;
using UnityEngine;

using Jaket.Assets;

/// <summary> Lobby controller with several useful methods. </summary>
public class LobbyController
{
    /// <summary> The current lobby the player is connected to. Null if the player is not connected to a lobby. </summary>
    public static Lobby? Lobby;
    /// <summary> Id of the last lobby owner, needed to track the exit of the host. </summary>
    public static SteamId LastOwner;
    /// <summary> Id of the last kicked player. </summary>
    public static SteamId LastKicked;

    /// <summary> Whether a lobby is creating right now. </summary>
    public static bool CreatingLobby;
    /// <summary> Whether a list of public lobbies is being fetched right now. </summary>
    public static bool FetchingLobbies;
    /// <summary> Whether the player owns the lobby. </summary>
    public static bool IsOwner;

    /// <summary> Whether PvP is allowed in this lobby. </summary>
    public static bool PvPAllowed => Lobby?.GetData("pvp") == "True";
    /// <summary> Whether cheats are allowed in this lobby. </summary>
    public static bool CheatsAllowed => Lobby?.GetData("cheats") == "True";
    /// <summary> Number of percentages that will be added to the boss's health for each player. </summary>
    public static float PPP;

    /// <summary> Scales health to increase difficulty. </summary>
    public static void ScaleHealth(ref float health) => health *= 1f + (Lobby == null ? 0f : Lobby.Value.MemberCount - 1f) * PPP;

    /// <summary> Whether the given lobby is created via Multikill. </summary>
    public static bool IsMultikillLobby(Lobby lobby) => lobby.Data.Any(pair => pair.Key == "mk_lobby");

    /// <summary> Creates the necessary listeners for proper work with a lobby. </summary>
    public static void Load()
    {
        // get the owner id when entering the lobby
        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            if (lobby.Owner.Id != 0L) LastOwner = lobby.Owner.Id;
            if (IsMultikillLobby(lobby))
            {
                LeaveLobby();
                Bundle.Hud("lobby.mk");
            }
        };

        // and leave the lobby if the owner has left it
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
        {
            if (member.Id == LastOwner) LeaveLobby();
        };

        // put the level name in the lobby data so that it can be seen in the public lobbies list
        Events.OnLoaded += () => Lobby?.SetData("level", MapMap(Tools.Scene));

        // if the player exits to the main menu, then this is equivalent to leaving the lobby
        Events.OnMainMenuLoaded += LeaveLobby;
    }

    #region control

    /// <summary> Asynchronously creates a new lobby and connects to it. </summary>
    public static void CreateLobby()
    {
        if (Lobby != null || CreatingLobby) return;

        var task = SteamMatchmaking.CreateLobbyAsync(8);
        CreatingLobby = true;

        task.GetAwaiter().OnCompleted(() =>
        {
            Lobby = task.Result.Value;
            IsOwner = true;

            Lobby?.SetJoinable(true);
            Lobby?.SetPrivate();
            Lobby?.SetData("jaket", "true");
            Lobby?.SetData("name", $"{SteamClient.Name}'s Lobby");
            Lobby?.SetData("level", MapMap(Tools.Scene));
            Lobby?.SetData("pvp", "True"); Lobby?.SetData("cheats", "True");

            CreatingLobby = false;
            Events.OnLobbyAction.Fire();
        });
    }

    /// <summary> Leaves the lobby, if the player is the owner, then all other players will be thrown into the main menu. </summary>
    public static void LeaveLobby()
    {
        // this is necessary in order to free up resources allocated for unread packets, otherwise there may be ghost players in the chat
        if (Lobby != null)
        {
            Networking.Server.Close();
            Networking.Client.Close();

            Lobby?.Leave();
            Lobby = null;
        }

        // if the client has left the lobby, then load the main menu
        if (!IsOwner && Tools.Scene != "Main Menu") Tools.Load("Main Menu");

        Networking.Clear();
        Events.OnLobbyAction.Fire();
    }

    /// <summary> Opens a steam overlay with a selection of a friend to invite to the lobby. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Asynchronously connects the player to the given lobby. </summary>
    public static async void JoinLobby(Lobby lobby)
    {
        if (Lobby?.Id == lobby.Id) { Bundle.Hud("lobby.join-yourself"); return; }

        if (Lobby != null) LeaveLobby();
        Debug.Log("Joining to the lobby...");

        var enter = await lobby.Join();
        if (enter == RoomEnter.Success)
        {
            Lobby = lobby;
            IsOwner = false;
        }
        else Bundle.Hud("lobby.closed");

        Events.OnLobbyAction.Fire();
    }

    #endregion
    #region members

    public static bool Contains(SteamId id)
    {
        if (Lobby == null) return false;

        foreach (var member in Lobby?.Members)
            if (member.Id == id) return true;

        return false;
    }

    /// <summary> Iterates each lobby member. </summary>
    public static void EachMember(Action<Friend> cons)
    {
        foreach (var member in Lobby.Value.Members) cons(member);
    }

    #endregion
    #region codes

    /// <summary> Copies the lobby code to the clipboard. </summary>
    public static void CopyCode()
    {
        GUIUtility.systemCopyBuffer = Lobby?.Id.ToString();
        if (Lobby != null) Bundle.Hud("lobby.copied");
    }

    /// <summary> Joins by the lobby code from the clipboard. </summary>
    public static void JoinByCode()
    {
        if (ulong.TryParse(GUIUtility.systemCopyBuffer, out var code)) JoinLobby(new(code));
        else Bundle.Hud("lobby.failed");
    }

    #endregion
    #region browser

    /// <summary> Asynchronously fetches a list of public lobbies. </summary>
    public static void FetchLobbies(Action<Lobby[]> done)
    {
        var task = SteamMatchmaking.LobbyList.RequestAsync();
        FetchingLobbies = true;

        task.GetAwaiter().OnCompleted(() =>
        {
            FetchingLobbies = false;
            done(task.Result.Where(l => l.Data.Any(pair => pair.Key == "jaket" || pair.Key == "mk_lobby")).ToArray());
        });
    }

    /// <summary> Maps the maps names so that they are more understandable to the average player. </summary>
    public static string MapMap(string map) => map switch
    {
        "Tutorial" => "Tutorial",
        "uk_construct" => "Sandbox",
        "Endless" => "Cyber Grind",
        "CreditsMuseum2" => "Museum",
        _ => map.Substring("Level ".Length)
    };

    #endregion
}
