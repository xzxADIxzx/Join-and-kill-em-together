namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
using Jaket.Sprays;
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
            var type = r.Enum<EntityType>();

            // player can only have one doll and its id should match the player's id
            if ((id == sender && type != EntityType.Player) || (id != sender && type == EntityType.Player)) return;

            if (ents[id] == null)
            {
                // double-check on cheats just in case of any custom multiplayer clients existence
                if (!Administration.CheatsAllowed && (type.IsEnemy() || type.IsItem())) return;

                // client cannot create special enemies
                if (type.IsEnemy() /* && !type.IsCommonEnemy() */) return;

                Administration.Handle(sender, ents[id] = Entities.Get(id, type));
            }
            ents[id]?.Read(r);
        });

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
        Listen(PacketType.DamageEntity, (con, sender, r, size) =>
        {
            if (ents.TryGetValue(r.Id(), out var entity))
            {
                entity?.Damage(r);
                Redirect(r, size, con);
            }
        });
        Listen(PacketType.KillEntity, (con, sender, r, s) =>
        {
            if (ents.TryGetValue(r.Id(), out var entity) && entity && entity is not RemotePlayer && entity is not LocalPlayer)
            {
                entity.Kill(r);
                Redirect(r, s, con);
            }
        });

        ListenAndRedirect(PacketType.Style, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Doll.ReadSuit(r);
        });
        ListenAndRedirect(PacketType.Punch, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Punch(r);
        });
        ListenAndRedirect(PacketType.Point, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Point(r);
        });

        ListenAndRedirect(PacketType.Spray, r => SprayManager.Spawn(r.Id(), r.Vector(), r.Vector()));

        Listen(PacketType.ImageChunk, (con, sender, r, s) =>
        {
            var owner = r.Id(); r.Position = 1; // extract the spray owner

            // prevent an attempt to overwrite someone else's spray, because this can lead to tragic consequences
            if (sender != owner)
            {
                Administration.Ban(sender);
                Log.Warning($"[Server] {sender} was blocked due to an attempt to overwrite someone else's spray");
            }
            else
            {
                SprayDistributor.Download(r);
                Redirect(r, s, con);
            }
        });

        Listen(PacketType.RequestImage, (con, sender, r, s) =>
        {
            var owner = r.Id();
            if (SprayDistributor.Requests.TryGetValue(owner, out var list)) list.Add(con);
            else
            {
                list = new();
                list.Add(con);
                SprayDistributor.Requests.Add(owner, list);
            }

            Log.Debug($"[Server] Got an image request for spray#{owner}. Count: {list.Count}");
        });

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
    }

    public override void Update()
    {
        Stats.MeasureTime(ref Stats.ReadTime, () => Manager.Receive());
        Stats.MeasureTime(ref Stats.WriteTime, () =>
        {
            if (Networking.Loading) return;
            ents.Pool(pool = ++pool % 4, e => Networking.Send(PacketType.Snapshot, 5 + e.BufferSize, w =>
            {
                w.Id(e.Id);
                w.Enum(e.Type);
                e.Write(w);
            }));
        });

        Manager.Connected.Each(c => c.Flush());
        Pointers.Free();
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
        if (netId.IsSteamId && Networking.Connections.Any(c => c.ConnectionName == accId.ToString()))
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

        // this will be used later to find the connection by id
        con.ConnectionName = accId.ToString();
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

        Handle(con, accId, data, size);
    }

    #endregion
}
