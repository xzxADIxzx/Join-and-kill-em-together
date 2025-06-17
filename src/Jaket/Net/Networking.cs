namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Endpoints;
using Jaket.Net.Types;
using Jaket.UI.Dialogs;

/// <summary> Class responsible for updating endpoints, transmitting packets and managing entities. </summary>
public class Networking
{
    /// <summary> Number of snapshots to be sent per second. </summary>
    public const int TICKS_PER_SECOND = 15;
    /// <summary> Number of subticks in a tick, i.e. each tick is divided into equal gaps in which snapshots of equal number of entities are written. </summary>
    public const int SUBTICKS_PER_TICK = 4;

    /// <summary> Server endpoint. Will be updated by the owner of the lobby. </summary>
    public static Server Server = new();
    /// <summary> Client endpoint. Will be updated by players connected to the lobby. </summary>
    public static Client Client = new();

    /// <summary> List of all entities by their id. May contain null. </summary>
    public static Pools Entities = new();
    /// <summary> Local player singleton. </summary>
    public static LocalPlayer LocalPlayer;

    /// <summary> Whether a scene is loading right now. </summary>
    public static bool Loading;
    /// <summary> Whether multiplayer was used in the current level. </summary>
    public static bool WasMultiplayerUsed;

    /// <summary> Returns the list of all entities. </summary>
    public static Entity[] Dump
    {
        get
        {
            var list = new Entity[Entities.Count()];
            int i = 0;
            Entities.Each(entry => list[i++] = entry.Value);
            return list;
        }
    }

    /// <summary> Loads server, client and event listeners. </summary>
    public static void Load()
    {
        Server.Load();
        Client.Load();

        LocalPlayer = Create<LocalPlayer>("Local Player");

        Events.EveryTick += NetworkUpdate;
        Events.EveryDozen += Optimize;

        Events.OnLoad += () => WasMultiplayerUsed = LobbyController.Online;
        Events.OnLobbyAction += () => WasMultiplayerUsed |= LobbyController.Online;

        Events.OnLoadingStart += () =>
        {
            if (LobbyController.Online) SceneHelper.SetLoadingSubtext(Random.value < .042f ? "I love you" : "/// MULTIPLAYER VIA JAKET ///");
            Loading = true;
        };
        Events.OnLoad += () =>
        {
            Clear();
            Loading = false;
        };

        // fires when accepting an invitation via the Steam overlay
        Events.OnLobbyInvite += LobbyController.JoinLobby;

        Events.OnLobbyEnter += () =>
        {
            Clear(); // destroy all entities, since the player could join from another lobby
            if (LobbyController.IsOwner)
            {
                // open the server so people can join it
                Server.Open();
            }
            else
            {
                // establishing a connection with the owner of the lobby
                Client.Connect(LobbyController.Lobby.Value.Owner.Id);
                // prevent objects from loading before the scene is loaded
                Loading = true;
            }
        };

        Events.OnMemberJoin += member =>
        {
            if (!Administration.Banned.Contains(member.Id.AccountId)) Bundle.Msg("player.joined", member.Name);
        };

        Events.OnMemberLeave += member =>
        {
            if (!Administration.Banned.Contains(member.Id.AccountId)) Bundle.Msg("player.left", member.Name);
        };

        Events.OnMemberLeave += member =>
        {
            // return the exited player's entities back to the host & close the connection
            if (!LobbyController.IsOwner) return;

            Find(member.Id.AccountId).Close();
            Entities.Alive(entity =>
            {
                if (entity is OwnableEntity oe && oe.Owner == member.Id.AccountId) oe.TakeOwnage();
            });
        };

        SteamMatchmaking.OnChatMessage += (lobby, member, message) =>
        {
            if (Administration.Banned.Contains(member.Id.AccountId)) return;
            if (message.Length > Chat.MAX_MESSAGE_LENGTH + 8) message = message.Substring(0, Chat.MAX_MESSAGE_LENGTH);

            if (message == "#/d")
            {
                Bundle.Msg("player.died", member.Name);
                if (LobbyConfig.HealBosses) Entities.Alive(entity =>
                {
                    if (entity is Enemy enemy && enemy.IsBoss) enemy.HealBoss();
                });
            }

            else if (message.StartsWith("#/k") && uint.TryParse(message.Substring(3), out uint id))
                Bundle.Msg("player.banned", Name(id));

            else if (message.StartsWith("#/s") && byte.TryParse(message.Substring(3), out byte team))
            {
                if (LocalPlayer.Team == (Team)team) StyleHUD.Instance.AddPoints(Mathf.RoundToInt(250f * StyleCalculator.Instance.airTime), "<color=#32CD32>FRATRICIDE</color>");
            }

            else if (message.StartsWith("#/r") && byte.TryParse(message.Substring(3), out byte rps))
                Chat.Instance.Receive($"[#FFA500]{member.Name} has chosen {rps switch { 0 => "rock", 1 => "paper", 2 => "scissors", _ => "nothing" }}");

            else if (message.StartsWith("/tts "))
                Chat.Instance.ReceiveTTS(GetTeamColor(member), member, message.Substring(5));
            else
                Chat.Instance.Receive(GetTeamColor(member), member.Name.Replace("[", "\\["), message);
        };
    }

    /// <summary> Kills all players and clears the list of entities. </summary>
    public static void Clear()
    {
        Entities.Player(player => player.Kill());
        Entities.Clear();
        Entities[LocalPlayer.Id] = LocalPlayer;
    }

    // TODO docs
    public static void Close()
    {
        Server.Close();
        Client.Close();
        Clear();
        Pointers.Free();
    }

    /// <summary> Core network logic should have been here, but in fact it is located in the server and client classes. </summary>
    public static void NetworkUpdate()
    {
        // the player isn't connected to the lobby and the logic doesn't need to be updated
        if (LobbyController.Offline) return;

        // update the server or client depending on the role of the player
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    /// <summary> Optimizes the network by removing the dead entities from the global list. </summary>
    public static void Optimize()
    {
        // there is no need to optimize the network if no one uses it
        if (LobbyController.Offline || DeadEntity.Instance.LastUpdate > Time.time - 1f) return;

        List<uint> toRemove = new();
        Entities.Each(pair =>
        {
            if (pair.Value == DeadEntity.Instance) toRemove.Add(pair.Key);
        });
        toRemove.ForEach(Entities.Remove);
    }

    #region tools

    /// <summary> Sends the packet to the given connection and updates statistics. </summary>
    public static void Send(Connection con, Ptr data, int size) => con.SendMessage(data, Stats.Write += size);

    /// <summary> Reserves memory for a packet, writes the data there, and then redirects it. </summary>
    public static void Send(PacketType type, int bytesCount = 47, Cons<Writer> data = null, Cons<Ptr, int> packet = null)
    {
        Writer w = new(Pointers.Allocate(data == null ? 1 : bytesCount + 1));

        w.Enum(type);
        data?.Invoke(w);

        packet ??= Redirect;
        packet(w.Memory, bytesCount);
    }

    /// <summary> Forwards the packet to either all of the clients or host. </summary>
    public static void Redirect(Ptr data, int size) => Connections.Each(c => Send(c, data, size));

    /// <summary> Finds a connection by the given identifier. </summary>
    public static Connection Find(uint id) => Connections.Find(c => c.ConnectionName == id.ToString());

    /// <summary> Returns the team of the given member. </summary>
    public static Team GetTeam(Friend friend) => friend.IsMe
        ? LocalPlayer.Team
        : Entities.TryGetValue(friend.Id.AccountId, out var entity) && entity != null && entity is RemotePlayer player ? player.Team : Team.Yellow;

    /// <summary> Returns the color of the given member's team. </summary>
    public static string GetTeamColor(Friend friend) => ColorUtility.ToHtmlStringRGBA(GetTeam(friend).Color());

    #endregion
}
