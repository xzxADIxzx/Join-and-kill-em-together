namespace Jaket.Net.EndPoints;

using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EntityTypes;

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
                int id = r.Int();
                Networking.CurrentOwner = r.Id();
                int type = r.Int();

                // if the entity is not in the list, add a new one with the given type or local if available
                if (entities.Count <= id) entities.Add(Networking.CurrentOwner == SteamClient.SteamId ? Networking.LocalPlayer : Entities.Get((EntityType)type));

                // sometimes players just get destroyed by the game, idk why
                if (entities[id] == null && type == (int)EntityType.Player) entities[id] = Entities.Get(EntityType.Player);

                // read entity data
                entities[id]?.Read(r);
            }
        });

        Listen(PacketType.HostLeft, r =>
        {
            LobbyController.LeaveLobby();
            SceneHelper.LoadScene("Main Menu");
        });

        Listen(PacketType.HostDied, r =>
        {
            // in the sandbox after death, enemies are not destroyed
            if (SceneHelper.CurrentScene == "uk_construct") return;

            entities.ForEach(entity =>
            {
                // destroy all enemies, because the host died and was thrown back to the checkpoint
                if (entity is RemoteEnemy && entity != null) Object.Destroy(entity.gameObject);
            });
        });

        Listen(PacketType.EnemyDied, r =>
        {
            // find the killed enemy in the list of entities
            var entity = entities[r.Int()];

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

        Listen(PacketType.SpawnBullet, r => Bullets.Read(r));

        Listen(PacketType.DamageEntity, r => entities[r.Int()]?.Damage(r));

        Listen(PacketType.OpenDoor, r => World.Instance.OpenDoor(r.Int()));

        // Listen(PacketType.UnlockDoors, r =>
        // {
        //     // find all the doors by tag, because it's faster than FindObjectsOfType
        //     foreach (var door in GameObject.FindGameObjectsWithTag("Door"))
        //     {
        //         // unlock them to prevent getting stuck in a room
        //         door.transform.parent.GetComponent<Door>()?.Unlock();
        //     }
        // });

        // Listen(PacketType.UnlockFinalDoor, r =>
        // {
        //     // find all the doors by tag, because it's faster than FindObjectsOfType
        //     foreach (var door in GameObject.FindGameObjectsWithTag("Door"))
        //     {
        //         // unlock the final door to prevent getting stuck in a room
        //         door.transform.parent.GetComponent<FinalDoor>()?.Open();
        //     }
        // });
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