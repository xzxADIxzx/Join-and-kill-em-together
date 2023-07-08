namespace Jaket.Net;

using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

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
}