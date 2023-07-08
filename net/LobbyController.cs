namespace Jaket.Net;

using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

using Jaket.UI;

public class LobbyController
{
    public static Lobby? Lobby;
    public static bool CreatingLobby;

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
                Lobby?.SetJoinable(true);
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
}