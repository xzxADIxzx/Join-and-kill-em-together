namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Admin;
using Jaket.Net.Endpoints;
using Jaket.Net.Types;
using Jaket.Sam;
using Jaket.UI;
using Jaket.UI.Dialogs;
using Jaket.World;

/// <summary> Class responsible for updating endpoints, transmitting packets and managing entities. </summary>
public static class Networking
{
    /// <summary> Number of snapshots to be sent per second. </summary>
    public const int TICKS_PER_SECOND = 15;
    /// <summary> Number of subticks in a tick, i.e. each tick is divided into equal gaps in which snapshots of equal number of entities are sent. </summary>
    public const int SUBTICKS_PER_TICK = 4;

    /// <summary> Server endpoint, updated by the owner of the lobby. </summary>
    public static Server Server = new();
    /// <summary> Client endpoint, updated by the members of the lobby. </summary>
    public static Client Client = new();

    /// <summary> Backbone of the entire network of entities. </summary>
    public static Pools Entities = new();
    /// <summary> Singleton of the local player. </summary>
    public static LocalPlayer LocalPlayer = new();

    /// <summary> Whether any scene is loading at the moment. </summary>
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
            Entities.Each(e => list[i++] = e);
            return list;
        }
    }

    /// <summary> Returns the list of all connections. </summary>
    public static IEnumerable<Connection> Connections
    {
        get
        {
            if (Server.Manager != null) foreach (var con in Server.Manager.Connected) yield return con;
            if (Client.Manager != null) yield return Client.Manager.Connection;
        }
    }

    #region general

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Server.Create();
        Client.Create();

        Events.EveryTick += Update;
        Events.EveryDozen += Optimize;

        Events.OnLoad        += () => WasMultiplayerUsed  = LobbyController.Online;
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

        Events.OnLobbyInvite += LobbyController.JoinLobby;

        Events.OnLobbyEnter += () =>
        {
            if (LobbyController.IsOwner)
            {
                // open the server so people can join
                Server.Open();
                // clear the pools so people join clean server
                Clear();
            }
            else
            {
                // establish a connection with the owner of the lobby
                Client.Connect(LobbyController.Lobby.Value.Owner.Id);
                // prevent objects from loading before the scene is loaded
                Loading = true;
            }
        };

        Events.OnMemberJoin += member =>
        {
            if (!Administration.Banned.Contains(member.AccId)) Bundle.Msg("player.joined", member.Name);
        };

        Events.OnMemberLeave += member =>
        {
            if (!Administration.Banned.Contains(member.AccId)) Bundle.Msg("player.left", member.Name);
        };

        Events.OnMemberLeave += member =>
        {
            if (LobbyController.IsOwner)
            {
                Connections.Each(c => c.UserData == member.AccId, c => c.Close());
                Entities.Alive<OwnableEntity>(e => e.Owner == member.AccId, e => e.TakeOwnage());
            }
        };

        SteamMatchmaking.OnChatMessage += (lobby, member, msg) =>
        {
            if (Administration.Banned.Contains(member.AccId)) return;
            if (msg.Length > Chat.MAX_LENGTH + 4) msg = msg[..Chat.MAX_LENGTH];

            string name = member.Name.Replace("[", "\\[");

            if (msg == "#/d")
            {
                Bundle.Msg("player.died", name);

                StyleHUD.Instance.AddPoints(Mathf.RoundToInt(420f * StyleCalculator.Instance.airTime), Bundle.Parse("[green]FRATRICIDE"));

                Gameflow.OnDeath(member);

                if (LobbyConfig.HealBosses) Entities.Alive<Enemy>(e => e.IsBoss, e => e.Heal());
            }

            else if (msg.StartsWith("#/w") && byte.TryParse(msg[3..], out byte wid) && lobby.Owner.Id == member.Id)
                Gameflow.OnVictory(wid);

            else if (msg.StartsWith("#/b") && uint.TryParse(msg[3..], out uint bid) && lobby.Owner.Id == member.Id)
                Bundle.Msg("player.banned", Name(bid));

            else if (msg.StartsWith("#/r") && byte.TryParse(msg[3..], out byte rps))
                Bundle.Msg("emote.roll", name, $"#emote.{rps}");

            else if (msg.StartsWith("#/t"))
            {
                if (member.IsMe)
                    SamAPI.TryPlay(msg = msg[3..], LocalPlayer.Voice);

                else if (Entities.TryGetValue(member.AccId, out var e) && e is RemotePlayer p)
                    SamAPI.TryPlay(msg = msg[3..], p.Voice);

                UI.Chat.Receive(msg, GetColor(member), name, Chat.TTS_TAG);
            }
            else
                UI.Chat.Receive(msg, GetColor(member), name);
        };
    }

    /// <summary> Updates network logic, i.e. receives incoming data and flushes outcoming one. </summary>
    public static void Update()
    {
        if (LobbyController.Offline) return;
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    /// <summary> Optimizes the pools by removing hidden entities, making the hashmap lighter. </summary>
    public static void Optimize()
    {
        if (LobbyController.Online) Entities.Each(e => Time.time - e.LastHidden >= 4f, e => Entities.Remove(e.Id));
    }

    /// <summary> Clears the pools, but pushes the local player back, as it must always be in. </summary>
    public static void Clear()
    {
        Entities.Player(p => p.Killed(default, -1));
        Entities.Clear();
        LocalPlayer.Push();
    }

    /// <summary> Closes all of the connections and clears the pools. </summary>
    public static void Close()
    {
        Clear();
        Server.Close();
        Client.Close();
    }

    #endregion
    #region tools

    /// <summary> Sends the packet to the given connection and updates statistics. </summary>
    public static void Send(Connection con, Ptr data, int size)
    {
        con.SendMessage(data, size);
        Stats.SentBs += size;
    }

    /// <summary> Reserves memory for a packet, writes the data there, and then redirects it. </summary>
    public static void Send(PacketType type, int bytesCount = 0, Cons<Writer> data = null, Cons<Ptr, int> packet = null)
    {
        Writer w = new(Pointers.Reserve(bytesCount = data == null ? 1 : bytesCount + 1));

        w.Enum(type);
        data?.Invoke(w);

        packet ??= Redirect;
        packet(w.Memory, bytesCount);
    }

    /// <summary> Forwards the packet to either all of the clients or host. </summary>
    public static void Redirect(Ptr data, int size) => Connections.Each(c => Send(c, data, size));

    /// <summary> Returns the team of the given member. </summary>
    public static Team GetTeam(Friend member) => member.IsMe
        ? LocalPlayer.Team
        : Entities.TryGetValue(member.AccId, out var entity) && entity != null && entity is RemotePlayer player ? player.Team : Team.Yellow;

    /// <summary> Returns the color of the given member's team. </summary>
    public static string GetColor(Friend member) => ColorUtility.ToHtmlStringRGBA(GetTeam(member).Color());

    #endregion
}
