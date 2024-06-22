namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
using Jaket.Sprays;
using Jaket.World;

/// <summary> Client endpoint processing socket events and host packets. </summary>
public class Client : Endpoint, IConnectionManager
{
    /// <summary> Steam networking sockets API backend. </summary>
    public ConnectionManager Manager { get; protected set; }

    public override void Load()
    {
        Listen(PacketType.Snapshot, r =>
        {
            var id = r.Id();
            var type = r.Enum<EntityType>();

            if (!ents.ContainsKey(id) || ents[id] == null) ents[id] = Entities.Get(id, type);
            ents[id]?.Read(r);
        });
        Listen(PacketType.Level, World.ReadData);
        Listen(PacketType.Ban, r => LobbyController.LeaveLobby());

        Listen(PacketType.SpawnBullet, Bullets.CInstantiate);
        Listen(PacketType.DamageEntity, r =>
        {
            if (ents.TryGetValue(r.Id(), out var entity)) entity?.Damage(r);
        });
        Listen(PacketType.KillEntity, r =>
        {
            if (ents.TryGetValue(r.Id(), out var entity)) entity?.Kill(r);
        });

        Listen(PacketType.Style, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Doll.ReadSuit(r);
        });
        Listen(PacketType.Punch, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Punch(r);
        });
        Listen(PacketType.Point, r =>
        {
            if (ents[r.Id()] is RemotePlayer player) player.Point(r);
        });

        Listen(PacketType.Spray, r => SprayManager.Spawn(r.Id(), r.Vector(), r.Vector()));

        Listen(PacketType.ImageChunk, SprayDistributor.Download);

        Listen(PacketType.ActivateObject, World.ReadAction);

        Listen(PacketType.CyberGrindAction, CyberGrind.LoadPattern);
    }

    public override void Update()
    {
        Stats.MeasureTime(ref Stats.ReadTime, () => Manager.Receive(256));
        Stats.MeasureTime(ref Stats.WriteTime, () =>
        {
            if (Networking.Loading) return;
            Networking.EachEntity(entity => entity.IsOwner, entity => Networking.Send(PacketType.Snapshot, w =>
            {
                w.Id(entity.Id);
                w.Enum(entity.Type);
                entity.Write(w);
            }));
        });

        // flush data
        Manager.Connection.Flush();
        Pointers.Free();
    }

    public override void Close() => Manager?.Close();

    public void Connect(SteamId id)
    {
        Manager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(id, 4242);
        Manager.Interface = this;
    }

    #region manager

    public void OnConnecting(ConnectionInfo info) => Log.Info("[Client] Connecting...");

    public void OnConnected(ConnectionInfo info) => Log.Info("[Client] Connected");

    public void OnDisconnected(ConnectionInfo info) => Log.Info("[Client] Disconnected");

    public void OnMessage(System.IntPtr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, LobbyController.LastOwner.AccountId, data, size);

    #endregion
}
