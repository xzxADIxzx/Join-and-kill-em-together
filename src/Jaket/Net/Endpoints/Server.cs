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
            // if the player does not have a doll, then create it
            if (!entities.ContainsKey(sender)) entities[sender] = Entities.Get(sender, EntityType.Player);

            // sometimes players disappear for some unknown reason, and sometimes I destroy them myself
            if (entities[sender] == null) entities[sender] = Entities.Get(sender, EntityType.Player);

            // read player data
            entities[sender]?.Read(r);

            // read item data if available
            if (r.Bool() && entities.TryGetValue(r.Id(), out var entity) && entity is Item item && item != null) item.Read(r);
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
        // the legend about NetIdentity Identity => this.address instead of this.identity
        var identity = Networking.GetIdentity(info);

        // multiple connections are prohibited
        if (identity.IsSteamId && Networking.FindCon(identity.SteamId).HasValue)
        {
            con.Close();
            return;
        }

        // this will be used later to find the connection by the id
        con.ConnectionName = identity.SteamId.ToString();

        // only steam users in the lobby can connect to the server
        if (identity.IsSteamId && LobbyController.Contains(identity.SteamId))
            con.Accept();
        else
            con.Close();
    }

    public void OnConnected(Connection con, ConnectionInfo info) =>
        Networking.Send(PacketType.LevelLoading, World.Instance.WriteData, (data, size) => con.SendMessage(data, size));

    public void OnDisconnected(Connection con, ConnectionInfo info) { }

    public void OnMessage(Connection con, NetIdentity id, System.IntPtr data, int size, long msg, long time, int channel) => Handle(con, id.SteamId, data, size);

    #endregion
}
