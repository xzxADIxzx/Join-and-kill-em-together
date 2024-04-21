namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Endpoints;
using Jaket.Net.Types;
using Jaket.UI.Dialogs;
using Jaket.World;

/// <summary> Class responsible for updating endpoints, transmitting packets and managing entities. </summary>
public class Networking
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
    public static Dictionary<uint, Entity> Entities = new();
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

        // create a local player to sync player data
        LocalPlayer = Tools.Create<LocalPlayer>("Local Player");
        // update network logic every tick
        Events.EveryTick += NetworkUpdate;

        Events.OnLoaded += () => WasMultiplayerUsed = LobbyController.Online;
        Events.OnLobbyAction += () => WasMultiplayerUsed |= LobbyController.Online;

        Events.OnLoaded += () =>
        {
            Clear(); // for safety
            Loading = false;

            // re-add the local player, because the list was cleared 
            Entities[LocalPlayer.Id] = LocalPlayer;
            // inform all players about the transition to a new level
            if (LobbyController.IsOwner)
            {
                World.Instance.Activated.Clear();
                Send(PacketType.LoadLevel, World.Instance.WriteData);
            }
        };

        // fires when accepting an invitation via the Steam overlay
        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => LobbyController.JoinLobby(lobby);

        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            Clear(); // destroy all entities, since the player could join from another lobby
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

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) => Bundle.Msg("player.joined", member.Name);

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
        {
            Bundle.Msg("player.left", member.Name);

            // kill the player doll and hide the nickname above
            if (Entities.TryGetValue(member.Id.AccountId, out var entity) && entity != null && entity is RemotePlayer player) player.Kill();

            if (!LobbyController.IsOwner) return;

            // returning the exited player's items back to the host owner & close the connection
            FindCon(member.Id)?.Close();
            EachItem(item =>
            {
                if (item.Owner == member.Id) item.TakeOwnage();
            });
        };

        SteamMatchmaking.OnChatMessage += (lobby, member, message) =>
        {
            if (message == "#/d")
                Bundle.Msg("player.died", member.Name);

            else if (message.StartsWith("#/k") && ulong.TryParse(message.Substring(3), out ulong id))
                Bundle.Msg("player.banned", new Friend(id).Name);

            else if (message.StartsWith("#/s") && byte.TryParse(message.Substring(3), out byte team))
            {
                if (LocalPlayer.Team == (Team)team) StyleHUD.Instance.AddPoints(Mathf.RoundToInt(250f * StyleCalculator.Instance.airTime), "<color=#32CD32>FRATRICIDE</color>");
            }

            else if (message.StartsWith("/tts "))
                Chat.Instance.ReceiveTTS(member, message.Substring(5));
            else
                Chat.Instance.Receive(GetTeamColor(member), member.Name.Replace("[", "\\["), message);
        };
    }

    /// <summary> Destroys all players and clears lists. </summary>
    public static void Clear()
    {
        EachPlayer(player => Tools.Destroy(player.gameObject));
        Entities.Clear();
    }

    /// <summary> Core network logic should have been here, but in fact it is located in the server and client classes. </summary>
    private static void NetworkUpdate()
    {
        // the player isn't connected to the lobby and the logic doesn't need to be updated
        if (LobbyController.Offline) return;

        // update the server or client depending on the role of the player
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    #region iteration

    /// <summary> Iterates each connection. </summary>
    public static void EachConnection(Action<Connection> cons)
    {
        foreach (var con in Server.Manager?.Connected) cons(con);
    }

    /// <summary> Iterates through each player observing the world. </summary>
    public static void EachObserver(Action<Vector3> cons)
    {
        cons(NewMovement.Instance.transform.position);
        EachPlayer(player => cons(player.transform.position));
    }

    /// <summary> Iterates each entity. </summary>
    public static void EachEntity(Action<Entity> cons)
    {
        foreach (var entity in Entities.Values)
            if (entity != null) cons(entity);
    }

    /// <summary> Iterates each entity the player owns. </summary>
    public static void EachOwned(Action<Entity> cons)
    {
        cons(LocalPlayer);
        foreach (var entity in Entities.Values)
            if (entity != null && entity is OwnableEntity ownable && ownable.IsOwner) cons(entity);
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
        : (Entities.TryGetValue(friend.Id.AccountId, out var entity) && entity != null && entity is RemotePlayer player ? player.Team : Team.Yellow);

    /// <summary> Returns the hex color of the friend's team. </summary>
    public static string GetTeamColor(Friend friend) => ColorUtility.ToHtmlStringRGBA(GetTeam(friend).Color());

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
            EachConnection(con => Tools.Send(con, data, size));
        else
            Tools.Send(Client.Manager.Connection, data, size);
    }

    /// <summary> Allocates memory, writes the packet there and sends it. </summary>
    public static void Send(PacketType packetType, Action<Writer> cons = null, Action<IntPtr, int> result = null, int size = 64) =>
        Writer.Write(w => { w.Enum(packetType); cons?.Invoke(w); }, result ?? Redirect, cons == null ? 1 : size + 1);

    #endregion
}
