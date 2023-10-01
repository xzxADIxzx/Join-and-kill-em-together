namespace Jaket.Net;

using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EndPoints;
using Jaket.Net.EntityTypes;
using Jaket.UI;
using UnityEngine.SceneManagement;

public class Networking : MonoBehaviour
{
    /// <summary> Number of snapshots to be sent per second. </summary>
    public const int SNAPSHOTS_PER_SECOND = 20;
    /// <summary> Number of seconds between snapshots. </summary>
    public const float SNAPSHOTS_SPACING = 1f / SNAPSHOTS_PER_SECOND;

    /// <summary> Server endpoint. Will be updated by the owner of the lobby. </summary>
    public static Server Server = new();
    /// <summary> Client endpoint. Will be updated by players connected to the lobby. </summary>
    public static Client Client = new();

    /// <summary> List of all entities synchronized between clients. May contain null if the entity was destroyed via Object.Destroy. </summary>
    public static Dictionary<ulong, Entity> Entities = new();
    /// <summary> List of all bosses on the map. Needed for the internal logic of the game. </summary>
    public static List<EnemyIdentifier> Bosses = new();

    /// <summary> Local player representation. </summary>
    public static LocalPlayer LocalPlayer;
    /// <summary> Whether a scene is loading right now. </summary>
    public static bool Loading;

    /// <summary> Loads server, client and event listeners. </summary>
    public static void Load()
    {
        Server.Load();
        Client.Load();

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // if the player exits to the main menu, then this is equivalent to leaving the lobby
            if (SceneHelper.CurrentScene == "Main Menu")
            {
                LobbyController.LeaveLobby();
                return;
            }

            Clear(); // for safety

            if (LobbyController.IsOwner)
            {
                // re-add a local player, because the list was cleared 
                Entities[LocalPlayer.Id] = LocalPlayer;

                // inform all players about the transition to a new level
                Redirect(Writer.Write(w => w.String(SceneHelper.CurrentScene)), PacketType.LevelLoading);
            }

            Loading = false;
        };

        // fires when accepting an invitation via the Steam overlay
        SteamFriends.OnGameLobbyJoinRequested += LobbyController.JoinLobby;

        SteamMatchmaking.OnChatMessage += (lobby, member, message) =>
        {
            if (message.StartsWith("<system>")) // I think it's okay
                Chat.Instance.ReceiveChatMessage(message.Substring("<system>".Length));
            else if (message.StartsWith("/tts "))
                Chat.Instance.ReceiveTTSMessage(member, message.Substring("/tts ".Length));
            else
                Chat.Instance.ReceiveChatMessage(GetTeamColor(member), member.Name, message);
        };

        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            // send some useful information to the chat so that players know about the mod's features
            Chat.Instance.Hello();

            // destroy all entities, since the player could join from another lobby
            Clear();

            if (LobbyController.IsOwner)
                // the lobby has just been created, so just add the local player to the list of entities
                Entities[LocalPlayer.Id] = LocalPlayer;
            else
            {
                // establishing a connection with the owner of the lobby
                SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);

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

            // confirm the connection with the player
            SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);

            // send the current scene name to the player
            Send(member.Id, Writer.Write(w => w.String(SceneHelper.CurrentScene)), PacketType.LevelLoading);
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
        {
            // send notification to chat
            if (LobbyController.IsOwner) lobby.SendChatString($"<system><color=red>Player {member.Name} left!</color>");

            // kill the player doll and hide the nickname above
            if (Entities.TryGetValue(member.Id, out var entity) && entity != null && entity is RemotePlayer player)
            {
                if (LobbyController.IsOwner) player.health.target = 0f;
                player.canvas.SetActive(false);

                // replace the entity with null so that the indicators no longer point to it
                Entities[member.Id] = null;
            }

            // remove the exited player indicator
            PlayerIndicators.Instance.Rebuild();
        };

        // create a local player to sync player data
        LocalPlayer = Utils.Object("Local Player", Plugin.Instance.transform).AddComponent<LocalPlayer>();

        // create an object to update the network logic
        Utils.Object("Networking", Plugin.Instance.transform).AddComponent<Networking>();
    }

    /// <summary> Destroys all network objects and clears lists. </summary>
    public static void Clear()
    {
        EachEntity(entity =>
        {
            // in no case can you destroy a local player
            if (entity != LocalPlayer) Destroy(entity.gameObject);
        });

        Entities.Clear();
        Bosses.Clear();
    }

    // TODO create a separate thread to increase fps?
    public void Awake() => InvokeRepeating("NetworkUpdate", 0f, SNAPSHOTS_SPACING);

    /// <summary> Core network logic. Part of it is moved to the server and the client. </summary>
    public void NetworkUpdate()
    {
        // the player isn't connected to the lobby, and no logic needs to be updated
        if (LobbyController.Lobby == null) return;

        // first level loading when connected to a lobby
        if (Loading)
        {
            if (SteamNetworking.IsP2PPacketAvailable((int)PacketType.LevelLoading))
            {
                var packet = SteamNetworking.ReadP2PPacket((int)PacketType.LevelLoading);
                if (packet.HasValue) Reader.Read(packet.Value.Data, r => SceneHelper.LoadScene(r.String()));
            }
            return; // during loading, look only for packages with the name of the desired scene
        }

        // update the server or client depending on the role of the player
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    #region iteration

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

    #endregion
    #region communication

    /// <summary> Sends packet data to the receiver over a reliable channel. </summary>
    public static void Send(SteamId receiver, byte[] data, PacketType packetType, P2PSend sendType = P2PSend.Reliable)
            => SteamNetworking.SendP2PPacket(receiver, data, nChannel: (int)packetType, sendType: sendType);

    /// <summary> Sends packet data to the receiver over an unreliable channel. </summary>
    public static void SendSnapshot(SteamId receiver, byte[] data)
            => Send(receiver, data, PacketType.Snapshot, P2PSend.Unreliable);

    /// <summary> Sends an empty packet to the receiver over a reliable channel. </summary>
    public static void SendEmpty(SteamId receiver, PacketType packetType)
            => Send(receiver, new byte[1], packetType);

    /// <summary> Forwards packet data to all clients or to a host that will forward to all clients. </summary>
    public static void Redirect(byte[] data, PacketType packetType)
    {
        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Send(member.Id, data, packetType));
        else
            Send(LobbyController.Owner, data, packetType);
    }

    #endregion
    #region teams

    /// <summary> Returns the team of the given friend. </summary>
    public static Team GetTeam(Friend friend) => friend.IsMe
        ? LocalPlayer.team
        : (Entities.TryGetValue(friend.Id, out var entity) && entity is RemotePlayer player ? player.team : Team.Yellow);

    /// <summary> Returns the hex color of the friend's team. </summary>
    public static string GetTeamColor(Friend friend) => ColorUtility.ToHtmlStringRGBA(GetTeam(friend).Data().Color());

    #endregion
}
