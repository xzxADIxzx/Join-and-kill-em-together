namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

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
            ulong id = r.Id();
            if (id == sender)
            {
                // sometimes I destroy players, sometimes they disappear for no reason
                if (!entities.ContainsKey(id) || entities[id] == null) entities[id] = Entities.Get(id, EntityType.Player);
                entities[id]?.Read(r);
            }
            else if (entities.TryGetValue(id, out var entity) && entity != null && entity is OwnableEntity ownable) ownable.Read(r);
        });

        Listen(PacketType.SpawnEntity, (con, sender, r) =>
        {
            var type = r.Enum<EntityType>();
            if (type.IsBullet() && Administration.CanSpawnEntityBullet(sender))
            {
                var bullet = Bullets.EInstantiate(type);

                bullet.transform.position = r.Vector();
                bullet.transform.eulerAngles = r.Vector();
                bullet.InitSpeed = r.Float();

                bullet.Owner = sender;
                bullet.OnTransferred();
                Administration.EntityBullets[sender].Add(bullet);
            }
            else if (type.IsEnemy() && LobbyController.CheatsAllowed)
            {
                var enemy = Enemies.Instantiate(type);
                enemy.transform.position = r.Vector();

                Administration.EnemySpawned(sender, enemy, type.IsBigEnemy());
            }
            else if (type.IsPlushy())
            {
                var plushy = Items.Instantiate(type);
                plushy.transform.position = r.Vector();

                Administration.PlushySpawned(sender, plushy);
            }
        });

        Listen(PacketType.SpawnBullet, (con, sender, r) =>
        {
            var type = r.Byte(); r.Position = 1; // extract the bullet type
            int cost = type == 4 ? 2 : type >= 17 && type <= 19 ? 8 : 1; // coin - 2, rail - 8, other - default

            if (Administration.CanSpawnCommonBullet(sender, cost))
            {
                Bullets.CInstantiate(r);
                Redirect(r, con);
            }
        });

        ListenAndRedirect(PacketType.DamageEntity, r => entities[r.Id()]?.Damage(r));

        Listen(PacketType.KillEntity, (con, sender, r) =>
        {
            var entity = entities[r.Id()];
            if (entity && entity is Bullet bullet && bullet.Owner == sender)
            {
                bullet.Kill();
                Redirect(r, con);
            }
        });

        ListenAndRedirect(PacketType.Style, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Style(r);
        });
        ListenAndRedirect(PacketType.Punch, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Punch(r);
        });
        ListenAndRedirect(PacketType.Point, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Point(r);
        });

        ListenAndRedirect(PacketType.Spray, r => SprayManager.CreateSpray(r.Id(), r.Vector(), r.Vector()));

        Listen(PacketType.ImageChunk, (con, sender, r) =>
        {
            var owner = r.Id(); r.Position = 1; // extract the spray owner

            // stop an attempt to overwrite someone else's spray, because this can lead to tragic consequences
            if (sender != owner)
            {
                Administration.Ban(sender);
                Log.Warning($"{sender} was blocked due to an attempt to overwrite someone else's spray");
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

        Listen(PacketType.ActivateObject, r => World.Instance.ReadAction(r));
    }

    public override void Update()
    {
        // read incoming data
        Manager.Receive(1024);

        // write data
        Networking.EachEntity(entity => Networking.Send(PacketType.Snapshot, w =>
        {
            w.Id(entity.Id);
            w.Enum(entity.Type);
            entity.Write(w);
        }));

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

        // multiple connections are prohibited
        if (identity.IsSteamId && Networking.FindCon(identity.SteamId).HasValue)
        {
            Log.Debug("[Server] Connection is rejected: already connected");
            con.Close();
            return;
        }

        // check if the player is banned
        if (identity.IsSteamId && Administration.Banned.Contains(identity.SteamId))
        {
            Log.Debug("[Server] Connection is rejected: banned");
            con.Close();
            return;
        }

        // this will be used later to find the connection by the id
        con.ConnectionName = identity.SteamId.ToString();

        // only steam users in the lobby can connect to the server
        if (identity.IsSteamId && LobbyController.Contains(identity.SteamId))
            con.Accept();
        else
        {
            Log.Debug("[Server] Connection rejected: either a non-steam user or not in the lobby");
            con.Close();
        }
    }

    public void OnConnected(Connection con, ConnectionInfo info)
    {
        Log.Info($"[Server] {info.Identity.SteamId} connected");
        Networking.Send(PacketType.LoadLevel, World.Instance.WriteData, (data, size) => con.SendMessage(data, size));
    }

    public void OnDisconnected(Connection con, ConnectionInfo info) => Log.Info($"[Server] {info.Identity.SteamId} disconnected");

    public void OnMessage(Connection con, NetIdentity id, System.IntPtr data, int size, long msg, long time, int channel) => Handle(con, id.SteamId, data, size);

    #endregion
}
