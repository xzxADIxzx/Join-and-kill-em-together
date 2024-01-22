namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.UI;

/// <summary> Lobby controller with several useful methods. </summary>
public class LobbyController
{
    /// <summary> The current lobby the player is connected to. Null if the player is not connected to a lobby. </summary>
    public static Lobby? Lobby;
    /// <summary> Lobby owner or the player's SteamID if the lobby is null. </summary>
    public static SteamId Owner => Lobby == null ? SteamClient.SteamId : Lobby.Value.Owner.Id;
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

    /// <summary> Creates the necessary listeners for proper work with a lobby. </summary>
    public static void Load()
    {
        // get the owner id when entering the lobby
        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            if (lobby.Owner.Id != 0L) LastOwner = lobby.Owner.Id;
        };

        // and leave the lobby if the owner has left it
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
        {
            if (member.Id == LastOwner) LeaveLobby();
        };

        // put the level name in the lobby data so that it can be seen in the public lobbies list
        Events.OnLoaded += () => Lobby?.SetData("level", MapMap(SceneHelper.CurrentScene));

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
            Lobby?.SetData("name", $"{SteamClient.Name}'s Lobby");
            Lobby?.SetData("level", MapMap(SceneHelper.CurrentScene));
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
        }

        Lobby?.Leave();
        Lobby = null;

        // if the client has left the lobby, then load the main menu
        if (!IsOwner && SceneHelper.CurrentScene != "Main Menu") SceneHelper.LoadScene("Main Menu");

        Networking.Clear(); // destroy all network objects
        Events.OnLobbyAction.Fire();
    }

    /// <summary> Opens a steam overlay with a selection of a friend to invite to the lobby. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Asynchronously connects the player to the given lobby. </summary>
    public static async void JoinLobby(Lobby lobby)
    {
        if (Lobby?.Id == lobby.Id)
        {
            UI.SendMsg(
@"""Why would you want to join yourself?!""
<size=20><color=grey>(c) xzxADIxzx</color></size>");
            return;
        }

        if (Lobby != null) LeaveLobby();
        Debug.Log("Joining to the lobby...");

        var enter = await lobby.Join();
        if (enter == RoomEnter.Success)
        {
            Lobby = lobby;
            IsOwner = false;
        }
        else UI.SendMsg(
@"<size=20><color=red>Couldn't connect to the lobby, it's a shame.</color></size>
Maybe it was closed or you were blocked ,_,");

        Events.OnLobbyAction.Fire();
    }

    #endregion
    #region members

    /// <summary> Kicks the member from the lobby, or rather asks him to leave, because Valve has not added such functionality to its API. </summary>
    public static void KickMember(Friend member)
    {
        // who does the client think he is?!
        if (!IsOwner) return;

        Networking.Send(PacketType.Kick, null, (data, size) => Networking.FindCon(LastKicked = member.Id)?.SendMessage(data, size));
        Lobby?.SendChatString($"<system><color=red>Player {member.Name} was kicked!</color>");
    }

    /// <summary> Returns a list of nicknames of players currently typing. </summary>
    public static List<string> TypingPlayers()
    {
        List<string> list = new();

        if (Chat.Shown) list.Add("You");
        Networking.EachPlayer(player =>
        {
            if (player.typing) list.Add(player.Header.Name);
        });

        return list;
    }

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

    /// <summary> Iterates each lobby member except its owner. </summary>
    public static void EachMemberExceptOwner(Action<Friend> cons)
    {
        foreach (var member in Lobby.Value.Members)
        {
            // usually this method is used by the server to send packets, because it doesn't make sense to send packets to itself
            if (member.Id != Lobby.Value.Owner.Id) cons(member);
        }
    }

    /// <summary> Iterates each lobby member, except for its owner and one more SteamID. </summary>
    public static void EachMemberExceptOwnerAnd(SteamId id, Action<Friend> cons)
    {
        foreach (var member in Lobby.Value.Members)
        {
            // usually this method is used by the server to forward packets from one of the clients
            if (member.Id != Lobby.Value.Owner.Id && member.Id != id) cons(member);
        }
    }

    #endregion
    #region codes

    /// <summary> Copies the lobby code to the clipboard. </summary>
    public static void CopyCode()
    {
        GUIUtility.systemCopyBuffer = Lobby?.Id.ToString();
        if (Lobby != null) UI.SendMsg(
@"<size=20><color=#00FF00>The lobby code has been successfully copied to the clipboard!</color></size>
Send it to your friends so they can join you :D");
    }

    /// <summary> Joins by the lobby code from the clipboard. </summary>
    public static void JoinByCode()
    {
        if (ulong.TryParse(GUIUtility.systemCopyBuffer, out var code)) JoinLobby(new(code));
        else UI.SendMsg(
@"<size=20><color=red>Could not find the lobby code on your clipboard!</color></size>
Make sure it is copied without spaces :(");
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
            done(task.Result);
        });
    }

    /// <summary> Maps the maps names so that they are more understandable to the average player. </summary>
    public static string MapMap(string map) => map switch
    {
        "Tutorial" => "Tutorial",
        "uk_construct" => "Sandbox",
        "Endless" => "Myth",
        "CreditsMuseum2" => "Museum",
        _ => map.Substring("Level ".Length)
    };

    #endregion
}