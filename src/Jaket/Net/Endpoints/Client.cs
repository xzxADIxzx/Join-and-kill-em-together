namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
using Jaket.Sprays;
using Jaket.World;

/// <summary> Endpoint of a client connection that processes socket events and server data. </summary>
public class Client : Endpoint, IConnectionManager
{
    static Pools ents => Networking.Entities;

    /// <summary> Steam networking sockets backend. </summary>
    public ConnectionManager Manager { get; private set; }

    public override void Create()
    {
        Listen(PacketType.Level, World.ReadData);

        Listen(PacketType.Snapshot, r =>
        {
            var id = r.Id();
            var type = r.EntityType();

            if (ents.TryGetValue(id, out var entity)) entity.Read(r);
            else
            {
                entity = Entities.Supply(id, type);

                entity.Read(r);
                entity.Create();
                entity.Push();
            }
        });

        /*
        Listen(PacketType.SpawnBullet, Bullets.CInstantiate);
        Listen(PacketType.DamageEntity, r =>
        {
            if (ents.TryGetValue(r.Id(), out var entity)) entity?.Damage(r);
        });
        Listen(PacketType.KillEntity, (con, sender, r, s) =>
        {
            if (ents.TryGetValue(r.Id(), out var entity)) entity?.Killed(r, s - 5);
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

        Listen(PacketType.Vote, r => Votes.UpdateVote(r.Id(), r.Byte()));
        */
    }

    public override void Update()
    {
        Stats.MeasureTime(ref Stats.ReadMs, () => Manager.Receive());
        Stats.MeasureTime(ref Stats.WriteMs, () =>
        {
            if (Networking.Loading) return;
            ents.ClientPool(pool = ++pool % 4, e => Networking.Send(PacketType.Snapshot, e.BufferSize, w =>
            {
                w.Id(e.Id);
                w.Enum(e.Type);
                e.Write(w);
            }));
        });
        Stats.MeasureTime(ref Stats.FlushMs, () =>
        {
            Manager.Connection.Flush();
            Pointers.Free();
        });
    }

    public override void Close()
    {
        Manager?.Close();
        Manager = null;
    }

    public void Connect(SteamId id)
    {
        Manager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(id, 4242);
        Manager.Interface = this;
    }

    #region manager

    public void OnConnecting(ConnectionInfo info) => Log.Info("[CLIENT] Connecting...");

    public void OnConnected(ConnectionInfo info) => Log.Info("[CLIENT] Connected");

    public void OnDisconnected(ConnectionInfo info) => Log.Info("[CLIENT] Disconnected");

    public void OnMessage(Ptr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, LobbyController.LastOwner, data, size);

    #endregion
}
