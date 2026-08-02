namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Admin;
using Jaket.Net.Types;
using Jaket.Sprays;
using Jaket.UI.Elements;
using Jaket.World;

/// <summary> Endpoint of a server connection that processes socket events and client data. </summary>
public class Server : Endpoint, ISocketManager
{
    static Pools ents => Networking.Entities;

    /// <summary> Steam networking sockets backend. </summary>
    public SocketManager Manager { get; private set; }

    public override void Create()
    {
        Listen(PacketType.Snapshot, (con, sender, r, s) =>
        {
            var id = r.Id();
            var type = r.EntityType();

            if ((id == sender) != (type == EntityType.Player)) return;

            if (ents.TryGetValue(id, out var entity)) entity.Read(r);
            else
            {
                entity = Entities.Supply(id, type);

                entity.Read(r);
                entity.Push();
                Events.Post(entity.Create);

                Administration.Find(sender).Handle(entity);
            }
        });

        Listen(PacketType.Hitscan, (con, sender, r, s) =>
        {
            var type = r.EntityType();
            var pos1 = r.Vector();
            var pos2 = r.Vector();
            var wall = r.Bool();
            var data = r.Byte();

            Events.Post(() => Entities.Hitscans.Make(type, pos1, pos2, wall, data));
            Redirect(r, s, con);

            Administration.Find(sender).Handle(type);
        });

        Listen(PacketType.Damage, (con, sender, r, s) =>
        {
            if (ents.TryGetValue(r.Id(), out var e))
            {
                e.Damage(r);
                Redirect(r, s, con);
            }
        });

        Listen(PacketType.Death, (con, sender, r, s) =>
        {
            if (ents.TryGetValue(r.Id(), out var e) && e is not LocalPlayer && e is not RemotePlayer)
            {
                e.Killed(r, s - 5);
                Redirect(r, s, con);
            }
        });

        Listen(PacketType.Style, (con, sender, r, s) =>
        {
            if (ents[sender] is RemotePlayer p && Redirect(ref r, s, con, sender)) p.Doll.ReadSuit(r);
        });

        Listen(PacketType.Punch, (con, sender, r, s) =>
        {
            if (ents[sender] is RemotePlayer p && Redirect(ref r, s, con, sender)) p.Punch(r);
        });

        Listen(PacketType.Point, (con, sender, r, s) =>
        {
            if (ents[sender] is RemotePlayer p && Redirect(ref r, s, con, sender))
            {
                if (p.Point) p.Point.Lifetime = 5.5f;
                p.Point = Point.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.Spray, (con, sender, r, s) =>
        {
            if (ents[sender] is RemotePlayer p && Redirect(ref r, s, con, sender))
            {
                if (p.Spray) p.Spray.Lifetime = 58f;
                p.Spray = Spray.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.ImageHeader, (con, sender, r, s) =>
        {
            if (Redirect(ref r, s, con, sender)) SprayDistributor.Download(sender, r.Int());
        });

        Listen(PacketType.ImageChunk, (con, sender, r, s) =>
        {
            if (Redirect(ref r, s, con, sender)) SprayDistributor.ProcessDownload(sender, s - 5, r);
        });

        Listen(PacketType.WorldAction, (con, sender, r, s) =>
        {
            var id = r.Byte();
            var p = r.Point();

            Events.Post(() => World.Perform(id, p));
            Redirect(r, s, con);
        });

        /*
        Listen(PacketType.Vote, (con, sender, r, s) =>
        {
            var owner = r.Id();

            // prevent an attempt to vote on behalf of another
            if (sender != owner)
            {
                Administration.Ban(sender);
                Log.Warning($"[Server] {sender} was blocked due to an attempt to vote on behalf of another");
            }
            else
            {
                Votes.UpdateVote(owner, r.Byte());
                Redirect(r, s, con);
            }
        });
        */
    }

    public override void Update()
    {
        Stats.Measure(ref Stats.Read, () => Manager.Receive());
        Stats.Measure(ref Stats.Write, () =>
        {
            if (Networking.Loading) return;
            ents.ServerPool(pool = ++pool % Networking.SUBTICKS_PER_TICK, Networking.SUBTICKS_PER_TICK, Networking.Send);
        });
        Manager.Connected.Each(c => c.Flush());
    }

    public override void Close()
    {
        Manager?.Close();
        Manager = null;
    }

    public void Open()
    {
        Manager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>(4242);
        Manager.Interface = this;
    }

    #region manager

    public void OnConnecting(Connection con, ConnectionInfo info)
    {
        Log.Info("[SERVER] Someone is connecting...");

        var netId = info.Identity;
        var accId = netId.SteamId.AccountId;

        // multiple connections are forbidden
        if (netId.IsSteamId && Networking.Connections.Any(c => c.UserData == accId))
        {
            Log.Debug("[SERVER] Connection is rejected: already connected");
            con.Close();
            return;
        }

        // restrain naughty players from rejoining
        if (netId.IsSteamId && Administration.Banned.Contains(accId))
        {
            Log.Debug("[SERVER] Connection is rejected: banned");
            con.Close();
            return;
        }

        // only the members of the lobby are allowed to connect
        if (netId.IsSteamId && !LobbyController.Contains(accId))
        {
            Log.Debug("[SERVER] Connection is rejected: either non-steam or not in the lobby");
            con.Close();
            return;
        }

        // this will be used later to find the connection by its id
        con.UserData = accId;
        con.Accept();
    }

    public void OnConnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[SERVER] {info.Identity.SteamId.AccountId} connected");
        Networking.Send(PacketType.Level, World.BufferSize, World.WriteData, (data, size) => Networking.Send(con, data, size));
    }

    public void OnDisconnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[SERVER] {info.Identity.SteamId.AccountId} disconnected");
        if (ents.TryGetValue(info.Identity.SteamId.AccountId, out var e) && e is RemotePlayer p) p.Kill();
    }

    public void OnMessage(Connection con, NetIdentity netId, Ptr data, int size, long msg, long time, int channel)
    {
        Handle(con, netId.SteamId.AccountId, data, size);
        Administration.Find(netId.SteamId.AccountId).Handle();
    }

    #endregion
}
