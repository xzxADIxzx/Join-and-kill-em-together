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

using static Jaket.UI.Lib.Pal;

/// <summary> Class responsible for updating endpoints, transmitting packets and managing entities. </summary>
public static class Networking
{
    /// <summary> Number of ticks per second. </summary>
    public const int TICKS_PER_SECOND = 30;
    /// <summary> Number of subticks per tick. </summary>
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
        Events.EveryHalf += Optimize;

        Events.OnLoadingStart += () =>
        {
            if (LobbyController.Online) SceneHelper.SetLoadingSubtext(Random.value < .042f ? "I love you" : "/// MULTIPLAYER VIA JAKET ///");
            Loading = true;
        };

        Events.OnLoad += () =>
        {
            Entities.Each(e => e != LocalPlayer && e is not RemotePlayer, e => e.Hidden = true);
            Loading = false;
        };

        Events.OnLobbyInvite += LobbyController.JoinLobby;

        Events.OnLobbyEnter += () =>
        {
            if (LobbyController.IsOwner)
                Server.Open();
            else
                Client.Connect(LobbyController.Lobby.Value.Owner.Id);

            Entities.Clear();
            LocalPlayer.Push();

            Loading = !LobbyController.IsOwner;
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

                Gameflow.OnDeath(member);

                if (LobbyConfig.HealBosses) Entities.Alive<Enemy>(e => e.Boss, e => e.Heal());
            }

            else if (msg.StartsWith("#/s") && uint.TryParse(msg[3..], out uint sid) && lobby.Owner.Id == member.Id)
                Gameflow.OnStart(sid);

            else if (msg.StartsWith("#/v") && byte.TryParse(msg[3..], out byte vid) && lobby.Owner.Id == member.Id)
                Gameflow.OnVictory(vid);

            else if (msg.StartsWith("#/b") && uint.TryParse(msg[3..], out uint bid) && lobby.Owner.Id == member.Id)
                Bundle.Msg("player.banned", bid.Name);

            else if (msg.StartsWith("#/r") && byte.TryParse(msg[3..], out byte rps))
                Bundle.Msg("emote.roll", name, $"#emote.{rps}");

            else if (msg.StartsWith("#/t"))
            {
                if (member.IsMe)
                    SamAPI.TryPlay(msg = msg[3..], LocalPlayer.Voice);

                else if (Entities[member.AccId] is RemotePlayer p)
                    SamAPI.TryPlay(msg = msg[3..], p.Voice);

                UI.Chat.Receive(msg, Int2Hex(member.Team.Color()), name, Chat.TTS_TAG);
            }
            else
                UI.Chat.Receive(msg, Int2Hex(member.Team.Color()), name);
        };
    }

    /// <summary> Updates network statistics and endpoints. </summary>
    private static void Update()
    {
        Stats.Reset();

        if (LobbyController.Offline) return;
        if (LobbyController.IsOwner)
            Server.Update();
        else
            Client.Update();
    }

    /// <summary> Optimizes the pools by removing entities. </summary>
    private static void Optimize()
    {
        if (LobbyController.Online) Entities.Each(e => Time.time - e.LastHidden >= 2f, e => Entities.Remove(e.Id));
    }

    #endregion
    #region packets

    /// <summary> Sends the given packet. </summary>
    public static void Send(Connection con, Ptr data, int size)
    {
        var result = con.SendMessage(data, size);
        if (result == Result.OK)
            Stats.Add(size);
        else
            Log.Error($"[NETW] Couldn't send a packet, the result is {result}");
    }

    /// <summary> Makes & sends a packet. </summary>
    public static void Send(PacketType type, int bytesCount, Cons<Writer> data, Cons<Ptr, int> cons = null)
    {
        if (bytesCount >= Pointers.RESERVED)
        {
            Log.Error($"[NETW] Couldn't send a packet, the size is {bytesCount}");
            return;
        }

        Writer w = new(Pointers.Allocated);

        w.Enum(type);
        data(w);

        (cons ?? Redirect)(w.Memory, 1 + bytesCount);
    }

    /// <summary> Sends the given entity. </summary>
    public static void Send(Entity entity)
    {
        Writer w = new(Pointers.Allocated);

        w.Enum(PacketType.Snapshot);
        w.Id(entity.Id);
        w.Enum(entity.Type);
        entity.Write(w);

        Redirect(w.Memory, 6 + entity.BufferSize);
    }

    /// <summary> Forwards the given packet to either all of the clients or the server. </summary>
    public static void Redirect(Ptr data, int size) => Connections.Each(c => Send(c, data, size));

    #endregion
}
