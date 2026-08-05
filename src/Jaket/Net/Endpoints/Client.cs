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

            if (ents[id] is Entity entity) entity.Read(r);
            else
            {
                entity = Entities.Supply(id, type);

                entity.Read(r);
                entity.Push();
                Events.Post(entity.Create);
            }
        });

        Listen(PacketType.Hitscan, r =>
        {
            var type = r.EntityType();
            var pos1 = r.Vector();
            var pos2 = r.Vector();
            var wall = r.Bool();
            var data = r.Byte();

            Events.Post(() => Entities.Hitscans.Make(type, pos1, pos2, wall, data));
        });

        Listen(PacketType.Damage, r =>
        {
            if (ents[r.Id()] is Entity e) e.Damage(r);
        });

        Listen(PacketType.Death, (con, sender, r, s) =>
        {
            if (ents[r.Id()] is Entity e) e.Killed(r, s - 5);
        });

        Listen(PacketType.Style, r =>
        {
            if (ents[r.Id()] is RemotePlayer p) p.Doll.ReadSuit(r);
        });

        Listen(PacketType.Punch, r =>
        {
            if (ents[r.Id()] is RemotePlayer p) p.Punch(r);
        });

        Listen(PacketType.Point, r =>
        {
            if (ents[r.Id()] is RemotePlayer p)
            {
                if (p.Point) p.Point.Lifetime = 5.5f;
                p.Point = Point.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.Spray, r =>
        {
            if (ents[r.Id()] is RemotePlayer p)
            {
                if (p.Spray) p.Spray.Lifetime = 58f;
                p.Spray = Spray.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.ImageHeader, r =>
        {
            SprayDistributor.Download(r.Id(), r.Int());
        });

        Listen(PacketType.ImageChunk, (con, sender, r, s) =>
        {
            SprayDistributor.ProcessDownload(r.Id(), s - 5, r);
        });

        Listen(PacketType.WorldAction, r =>
        {
            var id = r.Byte();
            var p = r.Point();

            Events.Post(() => World.Perform(id, p));
        });

        /*
        Listen(PacketType.Vote, r => Votes.UpdateVote(r.Id(), r.Byte()));
        */
    }

    public override void Update()
    {
        Stats.Measure(ref Stats.Read, () => Manager.Receive());
        Stats.Measure(ref Stats.Write, () =>
        {
            if (Networking.Loading) return;
            ents.ClientPool(ref pool, Networking.Send);
        });
        Manager.Connection.Flush();
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

    public void OnMessage(Ptr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, 0u, data, size);

    #endregion
}
