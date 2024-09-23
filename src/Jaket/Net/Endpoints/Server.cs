namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;
using System;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
using Jaket.Sprays;
using Jaket.World;

/// <summary> Host endpoint processing socket events and client packets. </summary>
public class Server : Endpoint, ISocketManager
{
    /// <summary> Steam networking sockets API backend. </summary>
    public SocketManager Manager { get; protected set; }

    public override void Load()
    {
        Listen(PacketType.Snapshot, (con, sender, r) =>
        {
            var id = r.Id();
            var type = r.Enum<EntityType>();

            // player can only have one doll and its id should match the player's id
            if ((id == sender && type != EntityType.Player) || (id != sender && type == EntityType.Player)) return;

            if (ents[id] == null)
            {
                // double-check on cheats just in case of any custom multiplayer clients existence
                if (!LobbyController.CheatsAllowed && (type.IsEnemy() || type.IsItem())) return;

                // client cannot create special enemies
                if (type.IsEnemy() && !type.IsCommonEnemy()) return;

                Administration.Handle(sender, ents[id] = Entities.Get(id, type));
            }
            ents[id]?.Read(r);
        });

        Listen(PacketType.SpawnBullet, (con, sender, r) =>
        {
            var type = r.Byte(); r.Position = 1; // extract the bullet type
            int cost = type >= 18 && type <= 20 ? 6 : 1; // rail costs more than the rest of the bullets

            if (type == 23 || Administration.CanSpawnBullet(sender, cost))
            {
                Bullets.CInstantiate(r);
                Redirect(r, con);
            }
        });
        Listen(PacketType.DamageEntity, (con, sender, r) =>
        {
            if (ents.TryGetValue(r.Id(), out var entity))
            {
                entity?.Damage(r);
                Redirect(r, con);
            }
        });
        Listen(PacketType.KillEntity, (con, sender, r) =>
        {
            if (ents.TryGetValue(r.Id(), out var entity) && entity && entity is not RemotePlayer && entity is not LocalPlayer)
            {
                entity.Kill(r);
                Redirect(r, con);
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

        Listen(PacketType.ImageChunk, (con, sender, r) =>
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
                Redirect(r, con);
            }
        });

        Listen(PacketType.RequestImage, (con, sender, r) =>
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

        Listen(PacketType.Vote, (con, sender, r) =>
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
                Redirect(r, con);
            }
        });
    }

    public override void Update()
    {
        Stats.MeasureTime(ref Stats.ReadTime, () => Manager.Receive(512));
        Stats.MeasureTime(ref Stats.WriteTime, () =>
        {
            if (Networking.Loading) return;
            Networking.Entities.Alive(entity => Networking.Send(PacketType.Snapshot, w =>
            {
                w.Id(entity.Id);
                w.Enum(entity.Type);
                entity.Write(w);
            }));
        });

        // flush data
        foreach (var con in Manager.Connected) con.Flush();
        Pointers.Free();
    }

    public override void Close() => Manager?.Close();

    public void Open()
    {
        Manager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>(4242);
        Manager.Interface = this;
    }

    #region manager

    public void OnConnecting(Connection con, ConnectionInfo info)
    {
        Log.Info("[Server] Someone is connecting...");
        var identity = info.Identity;
        var accId = identity.SteamId.AccountId;

        // multiple connections are prohibited
        if (identity.IsSteamId && Networking.FindCon(accId).HasValue)
        {
            Log.Debug("[Server] Connection is rejected: already connected");
            con.Close();
            return;
        }

        // check if the player is banned
        if (identity.IsSteamId && Administration.Banned.Contains(accId))
        {
            Log.Debug("[Server] Connection is rejected: banned");
            con.Close();
            return;
        }

        // this will be used later to find the connection by its id
        con.ConnectionName = accId.ToString();

        // only steam users in the lobby can connect to the server
        if (identity.IsSteamId && LobbyController.Contains(accId))
            con.Accept();
        else
        {
            Log.Debug("[Server] Connection rejected: either a non-steam user or not in the lobby");
            con.Close();
        }
    }

    public void OnConnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[Server] {info.Identity.SteamId.AccountId} connected");
        Networking.Send(PacketType.Level, World.WriteData, (data, size) => Tools.Send(con, data, size), size: 256);
    }

    public void OnDisconnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[Server] {info.Identity.SteamId.AccountId} disconnected");
        if (ents.TryGetValue(info.Identity.SteamId.AccountId, out var entity) && entity is RemotePlayer player) player?.NetKill();
    }

    public void OnMessage(Connection con, NetIdentity id, IntPtr data, int size, long msg, long time, int channel)
    {
        var accId = id.SteamId.AccountId;

        if (Administration.IsSpam(accId, size))
        {
            Administration.ClearSpam(accId);
            Log.Warning($"[Server] {accId} was warned due to sending a large amount of data");

            if (Administration.IsWarned(accId))
            {
                Administration.Ban(accId);
                Log.Warning($"[Server] {accId} was blocked due to an attempt to spam");
            }
        }

        Handle(con, accId, data, size);
    }

    #endregion
}
