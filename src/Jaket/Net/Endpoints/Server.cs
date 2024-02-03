namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
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

        Listen(PacketType.SpawnEntity, r =>
        {
            if (!LobbyController.CheatsAllowed) return;

            // the client asked to spawn the entity
            ulong id = Entities.NextId();
            var type = r.Enum<EntityType>();

            // but we need to make sure they can spawn it
            if (type.IsCommonEnemy() || type.IsPlushy()) return;
            var entity = Entities.Get(id, type);

            if (entity != null)
            {
                entity.transform.position = r.Vector();
                entities[id] = entity;
            }
        });

        ListenAndRedirect(PacketType.SpawnBullet, Bullets.CInstantiate);

        ListenAndRedirect(PacketType.DamageEntity, r => entities[r.Id()]?.Damage(r));

        ListenAndRedirect(PacketType.Punch, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Punch(r);
        });

        ListenAndRedirect(PacketType.Point, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Point(r);
        });
        ListenAndRedirect(PacketType.Spray, r => 
        {
            var id = r.Id();
            if (entities[id] is RemotePlayer player) player?.Spray(id, r);
        });
        ListenAndRedirect(PacketType.ImageChunk, SprayManager.LoadImageFromNetwork);

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
        Networking.Send(PacketType.LevelLoading, World.Instance.WriteData, (data, size) => con.SendMessage(data, size));
    }

    public void OnDisconnected(Connection con, ConnectionInfo info) => Log.Info($"[Server] {info.Identity.SteamId} disconnected");

    public void OnMessage(Connection con, NetIdentity id, System.IntPtr data, int size, long msg, long time, int channel) => Handle(con, id.SteamId, data, size);

    #endregion
}
