namespace Jaket.Net;

using System.Collections.Generic;
using Steamworks;
using UnityEngine;

using Jaket.UI;

public class Networking : MonoBehaviour
{
    public static List<Entity> entities = new List<Entity>();
    public static Dictionary<SteamId, RemotePlayer> players = new Dictionary<SteamId, RemotePlayer>();

    public static void Load()
    {
        SteamMatchmaking.OnChatMessage += (lobby, friend, message) => Chat.Received(friend.Name, message);
        SteamFriends.OnGameLobbyJoinRequested += LobbyController.JoinLobby;

        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            // the lobby has just been created, so nothing needs to be done
            if (LobbyController.IsOwner) return;

            // establishing a connection with the owner of the lobby
            SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);
        };

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) =>
        {
            // if you are not the owner of the lobby, then you do not need to do anything
            if (!LobbyController.IsOwner) return;

            // confirm the connection with the player
            SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);

            // create a new remote player doll
            var player = RemotePlayer.CreatePlayer();

            entities.Add(player);
            players.Add(friend.Id, player);
        };
    }
