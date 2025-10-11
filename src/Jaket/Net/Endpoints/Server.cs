namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
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
                entity.Create();
                entity.Push();

                Administration.Handle(sender, entity);
            }
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

        /*
        Listen(PacketType.SpawnBullet, (con, sender, r, s) =>
        {
            var type = r.Byte(); r.Position = 1; // extract the bullet type
            int cost = type >= 18 && type <= 20 ? 6 : 1; // rail costs more than the rest of the bullets

            if (type == 23 || Administration.CanSpawnBullet(sender, cost))
            {
                Bullets.CInstantiate(r);
                Redirect(r, s, con);
            }
        });

        ListenAndRedirect(PacketType.Spray, r => SprayManager.Spawn(r.Id(), r.Vector(), r.Vector()));

        ListenAndRedirect(PacketType.ActivateObject, World.ReadAction);

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
        Stats.MeasureTime(ref Stats.ReadMs, () => Manager.Receive());
        Stats.MeasureTime(ref Stats.WriteMs, () =>
        {
            if (Networking.Loading) return;
            ents.ServerPool(pool = ++pool % 4, e => Networking.Send(PacketType.Snapshot, e.BufferSize, w =>
            {
                w.Id(e.Id);
                w.Enum(e.Type);
                e.Write(w);
            }));
        });
        Stats.MeasureTime(ref Stats.FlushMs, () =>
        {
            Manager.Connected.Each(c => c.Flush());
            Pointers.Free();
        });
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
        Networking.Send(PacketType.Level, World.DataSize(), World.WriteData, (data, size) => Networking.Send(con, data, size));
    }

    public void OnDisconnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[SERVER] {info.Identity.SteamId.AccountId} disconnected");
        if (ents.TryGetValue(info.Identity.SteamId.AccountId, out var e) && e is RemotePlayer p) p.Kill();
    }

    public void OnMessage(Connection con, NetIdentity netId, Ptr data, int size, long msg, long time, int channel)
    {
        var accId = netId.SteamId.AccountId;

        /*
        if (Administration.IsSpam(accId, size))
        {
            Administration.ClearSpam(accId);
            Log.Warning($"[SERVER] {accId} was warned due to sending a large amount of data");

            if (Administration.IsWarned(accId))
            {
                Administration.Ban(accId);
                Log.Warning($"[SERVER] {accId} was blocked due to an attempt to spam");
            }
        }
        */

        Handle(con, accId, data, size);
    }

    #endregion
}
