namespace Jaket.Net;

using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Endpoints;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.World;

/// <summary> Class responsible for updating endpoints, transmitting packets and managing entities. </summary>
public class Networking : MonoSingleton<Networking>
{
    /// <summary> Number of snapshots to be sent per second. </summary>
    public const int SNAPSHOTS_PER_SECOND = 16;
    /// <summary> Number of seconds between snapshots. </summary>
    public const float SNAPSHOTS_SPACING = 1f / SNAPSHOTS_PER_SECOND;

    /// <summary> Server endpoint. Will be updated by the owner of the lobby. </summary>
    public static Server Server = new();
    /// <summary> Client endpoint. Will be updated by players connected to the lobby. </summary>
    public static Client Client = new();

    /// <summary> List of all entities by their id. May contain null. </summary>
    public static Dictionary<ulong, Entity> Entities = new();
    /// <summary> Local player singleton. </summary>
    public static LocalPlayer LocalPlayer;

    /// <summary> Whether a scene is loading right now. </summary>
    public static bool Loading;
    /// <summary> Whether multiplayer was used in the current level. </summary>
    public static bool WasMultiplayerUsed;

    /// <summary> Loads server, client and event listeners. </summary>
    public static void Load()
    {
        Server.Load();
        Client.Load();

        // create an object to update the network logic
        UI.Object("Networking").AddComponent<Networking>();
        // create a local player to sync player data
        LocalPlayer = UI.Object("Local Player", Instance.transform).AddComponent<LocalPlayer>();

        Events.OnLoaded += () => WasMultiplayerUsed = LobbyController.Lobby.HasValue;
        Events.OnLobbyAction += () => WasMultiplayerUsed |= LobbyController.Lobby.HasValue;

        Events.OnLoaded += () =>
        {
            Clear(); // for safety
            Loading = false;

            // re-add the local player, because the list was cleared 
            Entities[LocalPlayer.Id] = LocalPlayer;

            // inform all players about the transition to a new level
            if (LobbyController.IsOwner) Writer.Write(w =>
            {
                w.Enum(PacketType.LevelLoading);
                World.Instance.WriteData(w);
            }, Redirect);
        };

        // fires when accepting an invitation via the Steam overlay
        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => LobbyController.JoinLobby(lobby);

        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            // send some useful information to the chat so that players know about the mod's features
            Chat.Instance.Hello();
            // turn on player indicators & info because many don't even know about their existence
            PlayerIndicators.Instance.gameObject.SetActive(PlayerIndicators.Shown = true);
            if (!PlayerInfo.Shown) PlayerInfo.Instance.Toggle();

            Clear(); // destroy all entities, since the player could join from another lobby, and sync all items
            Items.SyncAll();

            if (LobbyController.IsOwner)
            {
                // open the server so people can join it
                Server.Open();

                // the lobby has just been created, so just add the local player to the list of entities
                Entities[LocalPlayer.Id] = LocalPlayer;
            }
            else
            {
                // establishing a connection with the owner of the lobby
                Client.Connect(lobby.Owner.Id);

                // prevent objects from loading before the scene is loaded
                Loading = true;
            }
        };

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) =>
        {
            // if you are not the owner of the lobby, then you do not need to do anything
            if (!LobbyController.IsOwner) return;

            // send notification to chat
            lobby.SendChatString($"<system><color=#00FF00>Player {member.Name} joined!</color>");

            // send the current scene name to the player
            Writer.Write(w =>
            {
                w.Enum(PacketType.LevelLoading);
                World.Instance.WriteData(w);
            }, (data, size) => FindCon(member.Id)?.SendMessage(data, size));
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
        {
            // send notification to chat
            if (LobbyController.IsOwner && LobbyController.LastKicked != member.Id)
                lobby.SendChatString($"<system><color=red>Player {member.Name} left!</color>");

            // kill the player doll and hide the nickname above
            if (Entities.TryGetValue(member.Id, out var entity) && entity != null && entity is RemotePlayer player)
            {
                player.health.target = 0f;
                player.Header.Hide();

                // replace the entity with null so that the indicators no longer point to it
                Entities[member.Id] = null;
            }

            // returning the exited player's items back to the host owner & close the connection
            if (LobbyController.IsOwner)
            {
                FindCon(member.Id)?.Close();
                EachItem(item =>
                {
                    if (item.Owner == member.Id) item.Owner = SteamClient.SteamId;
                });
            }
        };
    }

    /// <summary> Destroys all players and clears lists. </summary>
    public static void Clear()
    {
        EachPlayer(player => Destroy(player.gameObject));
        Entities.Clear();
    }

    #region cycle

    private void Start() => InvokeRepeating("NetworkUpdate", 0f, SNAPSHOTS_SPACING);

    /// <summary> Core network logic should have been here, but in fact it is located in the server and client classes. </summary>
    private void NetworkUpdate()
    {
        // the player isn't connected to the lobby, and no logic needs to be updated
        if (LobbyController.Lobby == null) return;

        // update the server or client depending on the role of the player
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    #endregion
    #region iteration

    /// <summary> Iterates each connection. </summary>
    public static void EachConnection(Action<Connection> cons)
    {
        foreach (var con in Server.Manager.Connected) cons(con);
    }

    /// <summary> Iterates each entity. </summary>
    public static void EachEntity(Action<Entity> cons)
    {
        foreach (var entity in Entities.Values)
            if (entity != null) cons(entity);
    }

    /// <summary> Iterates each player. </summary>
    public static void EachPlayer(Action<RemotePlayer> cons)
    {
        foreach (var entity in Entities.Values)
            if (entity != null && entity is RemotePlayer player) cons(player);
    }

    /// <summary> Iterates each item. </summary>
    public static void EachItem(Action<Item> cons)
    {
        foreach (var entity in Entities.Values)
            if (entity != null && entity is Item item) cons(item);
    }

    #endregion
    #region tools

    /// <summary> Returns the team of the given friend. </summary>
    public static Team GetTeam(Friend friend) => friend.IsMe
        ? LocalPlayer.Team
        : (Entities.TryGetValue(friend.Id, out var entity) && entity != null && entity is RemotePlayer player ? player.team : Team.Yellow);

    /// <summary> Returns the hex color of the friend's team. </summary>
    public static string GetTeamColor(Friend friend) => ColorUtility.ToHtmlStringRGBA(GetTeam(friend).Data().Color());

    /// <summary> This class is a little broken, so you have to use crutches. </summary>
    public static NetIdentity GetIdentity(ConnectionInfo info) => (NetIdentity)AccessTools.DeclaredField(typeof(ConnectionInfo), 0).GetValue(info);

    /// <summary> Finds a connection by id or returns null if there is no such connection. </summary>
    public static Connection? FindCon(SteamId id)
    {
        foreach (var con in Server.Manager.Connected)
            if (con.ConnectionName == id.ToString()) return con;
        return null;
    }

    /// <summary> Forwards the packet to all clients or the host. </summary>
    public static void Redirect(IntPtr data, int size)
    {
        if (LobbyController.IsOwner)
            EachConnection(con => con.SendMessage(data, size));
        else
            Client.Manager.Connection.SendMessage(data, size);
    }

    #endregion
}
