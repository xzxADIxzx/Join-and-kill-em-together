namespace Jaket.Net.EndPoints;

using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EntityTypes;
using Jaket.World;

/// <summary> Endpoint of the client connected to the host. </summary>
public class Client : Endpoint
{
    public override void Load()
    {
        Listen(PacketType.Snapshot, r =>
        {
            // each snapshot contains all the entities, so you need to read them all
            while (r.Position < r.Length)
            {
                ulong id = r.Id();
                EntityType type = (EntityType)r.Byte();

                // if the entity is not in the list, add a new one with the given type or local if available
                if (!entities.ContainsKey(id)) entities[id] = id == SteamClient.SteamId ? Networking.LocalPlayer : Entities.Get(id, type);

                // after respawn, Leviathan may be absent, so it must be returned if possible
                if (entities[id] == null && type == EntityType.Leviathan)
                {
                    entities[id] = Entities.Get(id, EntityType.Leviathan);
                    if (entities[id] == null) r.Bytes(40);
                }

                // sometimes players disappear for some unknown reason, and sometimes I destroy them myself
                if (entities[id] == null && type == EntityType.Player) entities[id] = Entities.Get(id, EntityType.Player);

                // read entity data
                entities[id]?.Read(r);
            }
        });

        Listen(PacketType.LevelLoading, r => SceneHelper.LoadScene(r.String()));

        Listen(PacketType.HostLeft, r =>
        {
            LobbyController.LeaveLobby();
            SceneHelper.LoadScene("Main Menu");
        });

        Listen(PacketType.HostDied, r =>
        {
            // in the sandbox after death, enemies are not destroyed
            if (SceneHelper.CurrentScene == "uk_construct") return;

            Networking.EachEntity(entity =>
            {
                // destroy all enemies, because the host died and was thrown back to the checkpoint
                if (entity is RemoteEnemy && entity != null) Object.Destroy(entity.gameObject);
            });
        });

        Listen(PacketType.EnemyDied, r =>
        {
            // find the killed enemy in the list of entities
            var entity = entities[r.Id()];

            // kill the enemy so that there is no desynchronization
            if (entity is RemoteEnemy enemy) enemy.enemyId.InstaKill();
        });

        Listen(PacketType.BossDefeated, r =>
        {
            // maybe sending the name is not the best idea, but I don't have any others
            string bossName = r.String();

            // find the original of the killed boss
            var boss = Networking.Bosses.Find(enemyId => enemyId != null && enemyId.gameObject.name == bossName);

            // kill the boss to trigger the internal logic of the game
            if (boss != null) Object.Destroy(boss.gameObject);
        });

        Listen(PacketType.SpawnBullet, Bullets.Read);

        Listen(PacketType.DamageEntity, r => entities[r.Id()]?.Damage(r));

        Listen(PacketType.OpenDoor, r => World.Instance.OpenDoor(r.Int()));

        Listen(PacketType.ActivateObject,r=>World.Instance.ActivateObject(r.Int()));
    }

    public override void Update()
    {
        // write player data
        byte[] data = Writer.Write(Networking.LocalPlayer.Write);

        // send player data
        Networking.SendSnapshot(LobbyController.Owner, data);

        // read incoming packets
        UpdateListeners();
    }
}