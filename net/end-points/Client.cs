namespace Jaket.Net.EndPoints;

using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EntityTypes;

public class Client : Endpoint
{
    public override void Load()
    {
        Listen(PacketType.Snapshot, (sender, r) =>
        {
            // each snapshot contains all the entities, so you need to read them all
            while (r.Position < r.Length)
            {
                int id = r.Int();
                Networking.CurrentOwner = r.Id();
                int type = r.Int();

                // if the entity is not in the list, add a new one with the given type or local if available
                if (entities.Count <= id) entities.Add(Networking.CurrentOwner == SteamClient.SteamId ? Networking.LocalPlayer : Entities.Get((EntityType)type));

                // read entity data
                entities[id].Read(r);
            }
        });

        Listen(PacketType.HostDied, (sender, r) =>
        {
            // in the sandbox after death, enemies are not destroyed
            if (SceneHelper.CurrentScene == "uk_construct") return;

            entities.ForEach(entity =>
            {
                // destroy all enemies, because the host died and was thrown back to the checkpoint
                if (entity is RemoteEnemy && entity != null) Object.Destroy(entity.gameObject);
            });
        });

        Listen(PacketType.SpawnBullet, (sender, r) => Bullets.Read(r));

        Listen(PacketType.DamagePlayer, (sender, r) => NewMovement.Instance.GetHurt((int)r.Float(), false, 0f));
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