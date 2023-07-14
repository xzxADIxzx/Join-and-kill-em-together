namespace Jaket.Net.EndPoints;

using Steamworks;

using Jaket.Content;
using Jaket.IO;

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

        Listen(PacketType.SpawnBullet, (sender, r) => Bullets.Read(r));

        Listen(PacketType.DamagePlayer, (sender, r) => NewMovement.Instance.GetHurt((int)r.Float(), false, 0f));
    }

    public override void Update()
    {
        // write player data
        byte[] data = Writer.Write(Networking.LocalPlayer.Write);

        // send player data
        Networking.SendSnapshot(LobbyController.Lobby.Value.Owner.Id, data);

        // read incoming packets
        UpdateListeners();
    }
}