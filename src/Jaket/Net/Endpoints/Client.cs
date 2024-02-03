namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;
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
            ulong id = r.Id();
            var type = r.Enum<EntityType>();

            // if the entity is not in the list, add a new one with the given type or local if available
            if (!entities.ContainsKey(id)) entities[id] = id == SteamClient.SteamId ? Networking.LocalPlayer : Entities.Get(id, type);

            // after respawn, Leviathan or hand may be absent, so it must be returned if possible
            // sometimes players disappear for some unknown reason, and sometimes I destroy them myself
            if (entities[id] == null && (type == EntityType.Hand || type == EntityType.Leviathan || type == EntityType.Player))
                entities[id] = Entities.Get(id, type);

            entities[id]?.Read(r);
        });

        Listen(PacketType.LoadLevel, r => World.Instance.ReadData(r));

        Listen(PacketType.Kick, r => LobbyController.LeaveLobby());

        Listen(PacketType.SpawnBullet, Bullets.CInstantiate);

        Listen(PacketType.DamageEntity, r => entities[r.Id()]?.Damage(r));

        Listen(PacketType.KillEntity, r => entities[r.Id()]?.Kill());

        Listen(PacketType.Punch, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Punch(r);
        });

        Listen(PacketType.Point, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Point(r);
        });

        Listen(PacketType.ActivateObject, r => World.Instance.ReadAction(r));

        Listen(PacketType.CinemaAction, r => Cinema.Play(r.String()));

        Listen(PacketType.CyberGrindAction, r => CyberGrind.Instance.LoadPattern(r));
    }

    public override void Update()
    {
        // read incoming data
        Manager.Receive(256); Manager.Receive(256); Manager.Receive(256); Manager.Receive(256); // WHY

        // write data
        Networking.EachOwned(entity => Networking.Send(PacketType.Snapshot, w =>
        {
            w.Id(entity.Id);
            entity.Write(w);
        }));

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

    public void OnMessage(System.IntPtr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, LobbyController.LastOwner, data, size);

    #endregion
}
