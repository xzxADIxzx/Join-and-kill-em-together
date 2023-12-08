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
            EntityType type = (EntityType)r.Byte();

            // if the entity is not in the list, add a new one with the given type or local if available
            if (!entities.ContainsKey(id)) entities[id] = id == SteamClient.SteamId ? Networking.LocalPlayer : Entities.Get(id, type);

            // after respawn, Leviathan or hand may be absent, so it must be returned if possible
            // sometimes players disappear for some unknown reason, and sometimes I destroy them myself
            if (entities[id] == null && (type == EntityType.Hand || type == EntityType.Leviathan || type == EntityType.Player))
                entities[id] = Entities.Get(id, type);

            // read entity data
            entities[id]?.Read(r);
        });

        Listen(PacketType.LevelLoading, r => World.Instance.ReadData(r)); // instance is null at client load time so arrow function is required

        Listen(PacketType.Kick, r => LobbyController.LeaveLobby());

        Listen(PacketType.HostDied, r =>
        {
            // in the sandbox after death, enemies are not destroyed
            if (SceneHelper.CurrentScene == "uk_construct") return;

            Networking.EachEntity(entity =>
            {
                // destroy all enemies, because the host died and was thrown back to the checkpoint
                if (entity is Enemy) Object.Destroy(entity.gameObject);
            });
        });

        Listen(PacketType.EnemyDied, r =>
        {
            // find the killed enemy in the list of entities
            var entity = entities[r.Id()];

            // kill the enemy so that there is no desynchronization
            if (entity is Enemy enemy) enemy?.Kill();
        });

        Listen(PacketType.SpawnBullet, Bullets.Read);

        Listen(PacketType.DamageEntity, r => entities[r.Id()]?.Damage(r));

        Listen(PacketType.Punch, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Punch(r);
        });

        Listen(PacketType.Point, r =>
        {
            if (entities[r.Id()] is RemotePlayer player) player?.Point(r);
        });

        Listen(PacketType.OpenDoor, r => World.Instance.OpenDoor(r.Int()));

        Listen(PacketType.ActivateObject, r => World.Instance.ActivateObject(r.Int()));

        Listen(PacketType.CinemaAction, r => Cinema.Play(r.String()));

        Listen(PacketType.CybergrindAction, r => CyberGrind.Instance.LoadPattern(r.Int(), r.String()));
    }

    public override void Update()
    {
        // read incoming data
        Manager.Receive(1024);

        // write data
        Writer.Write(w =>
        {
            w.Enum(PacketType.Snapshot);
            Networking.LocalPlayer.Write(w);
        }, Networking.Redirect);

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

    public void OnConnecting(ConnectionInfo info) { }

    public void OnConnected(ConnectionInfo info) { }

    public void OnDisconnected(ConnectionInfo info) { }

    public void OnMessage(System.IntPtr data, int size, long msg, long time, int channel) => Handle(Manager.Connection, LobbyController.LastOwner, data, size);

    #endregion
}
