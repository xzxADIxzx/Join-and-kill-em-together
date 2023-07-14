namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.UI;

/// <summary> Lobby controller with several useful methods. </summary>
public class LobbyController
{
    /// <summary> The current lobby the player is connected to. Null if the player is not connected to a lobby. </summary>
    public static Lobby? Lobby;
    /// <summary> Whether a lobby is creating right now. </summary>
    public static bool CreatingLobby;
    /// <summary> Whether the player owns the lobby. </summary>
    public static bool IsOwner;

    #region control

    /// <summary> Asynchronously creates a new lobby and connects to it. </summary>
    public static void CreateLobby(Action done)
    {
        if (Lobby != null || CreatingLobby) return;

        var task = SteamMatchmaking.CreateLobbyAsync(8);
        CreatingLobby = true;

        task.GetAwaiter().OnCompleted(() =>
        {
            if (task.Result.HasValue)
            {
                Lobby = task.Result.Value;
                IsOwner = true;

                Lobby?.SetJoinable(true);
                Lobby?.SetFriendsOnly();
            }

            CreatingLobby = false;
            done.Invoke();

            // update the discord activity so everyone can know I've been working hard
            DiscordController.Instance.FetchSceneActivity(SceneHelper.CurrentScene);
        });
    }

    // TODO disconnect all other players from the lobby if the player is the owner
    /// <summary> Leaves the lobby, if the player is the owner, then all other players will be thrown into the main menu. </summary>
    public static void LeaveLobby()
    {
        Lobby?.Leave();
        Lobby = null;

        Networking.players.Clear(); // TODO move to Networking
        Networking.entities.ForEach(entity =>
        {
            if (entity != Networking.LocalPlayer) GameObject.Destroy(entity.gameObject);
        });

        // remove mini-ads if the player is playing alone
        DiscordController.Instance.FetchSceneActivity(SceneHelper.CurrentScene);
    }

    /// <summary> Opens a steam overlay with a selection of a friend to invite to the lobby. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Asynchronously connects the player to the given lobby. </summary>
    public static async void JoinLobby(Lobby lobby, SteamId id)
    {
        Debug.Log("Joining to the lobby...");

        var enter = await lobby.Join();
        if (enter == RoomEnter.Success)
        {
            Lobby = lobby;
            IsOwner = false;
        }
        else Debug.LogError("Couldn't join the lobby.");

        // update the discord activity so everyone can know I've been working hard
        DiscordController.Instance.FetchSceneActivity(SceneHelper.CurrentScene);
    }

    #endregion
    #region members

    /// <summary> Returns a list of nicknames of players currently typing. </summary>
    public static List<string> TypingPlayers()
    {
        List<string> list = new();

        if (Chat.Shown) list.Add("You");
        foreach (var player in Networking.players.Values)
            if (player.typing) list.Add(player.nickname);

        return list;
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
}