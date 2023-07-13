namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.UI;

public class LobbyController
{
    public static Lobby? Lobby;
    public static bool CreatingLobby, IsOwner;

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
        });
    }

    public static void CloseLobby()
    {
        Lobby?.Leave();
        Lobby = null;
    }

    public static void InviteFriend()
    {
        if (Lobby != null) SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);
    }

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
    }

    public static List<string> TypingPlayers()
    {
        List<string> list = new();

        if (Chat.Shown) list.Add("You");
        foreach (var player in Networking.players.Values)
            if (player.typing) list.Add(player.nickname);

        return list;
    }
}