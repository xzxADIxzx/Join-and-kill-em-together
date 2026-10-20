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
    /// <summary> Steam networking sockets backend. </summary>
    public ConnectionManager Manager { get; private set; }

    public override bool Ready => Manager != null;

    public override void Create()
    {
        var ents = Networking.Entities;

        Listen(PacketType.Level, World.ReadData);

        Listen(PacketType.Image, (con, sender, r, s) =>
        {
            SprayDistributor.ProcessDownload(r.Id(), r.Int(), s - 9, r);
        });

        Listen(PacketType.Snapshot, r =>
        {
            var id = r.Id();
            var type = r.Entity();

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
            var type = r.Entity();
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
            if (ents[r.Id()] is RemotePlayer p) p.Doll.Read(r);
        });

        Listen(PacketType.Sound, r =>
        {
            if (ents[r.Id()] is RemotePlayer p) Entities.Players.Play(p, r.Byte(), r.Vector(), r.Vector());
        });

        Listen(PacketType.Point, r =>
        {
            if (ents[r.Id()] is RemotePlayer p)
            {
                p.Point?.Remove();
                p.Point = Point.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.Spray, r =>
        {
            if (ents[r.Id()] is RemotePlayer p)
            {
                p.Spray?.Remove();
                p.Spray = Spray.Spawn(r.Vector(), r.Vector(), p.Team, p);
            }
        });

        Listen(PacketType.WorldAction, r =>
        {
            var id = r.Byte();
            var p = r.Point();

            Events.Post(() => World.Perform(id, p));
        });
    }

    public override void Update()
    {
        Stats.Measure(ref Stats.Read, () => Manager.Receive());
        Stats.Measure(ref Stats.Write, () =>
        {
            if (Networking.Loading) return;
            Networking.Entities.ClientPool(pool, Networking.Send);
        });
        Manager.Connection.Flush();
    }

    public override void Close()
    {
        Manager?.Close();
        Manager = null;
    }

    public void Connect() => Manager = SteamNetworkingSockets.ConnectRelay(LobbyController.Lobby.Value.Owner.Id, 4242, this);

    #region manager

    public void OnConnecting(ConnectionInfo info) => Log.Info("[CLIENT] Connecting to someone...");

    public void OnConnected(ConnectionInfo info) => Log.Info($"[CLIENT] Connected to {info.Identity.SteamId.AccountId}");

    public void OnDisconnected(ConnectionInfo info) => Log.Info($"[CLIENT] Disconnected due to {info.EndReason}");

    public void OnMessage(Ptr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, 0u, data, size);

    #endregion
}
