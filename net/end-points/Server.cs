namespace Jaket.Net.EndPoints;

using Steamworks;

using Jaket.Content;
using Jaket.Net.EntityTypes;
using Jaket.IO;

public class Server : Endpoint
{
    public override void Load()
    {
        Listen(PacketType.Snapshot, (sender, r) =>
        {
            Networking.CurrentOwner = sender;

            // if the player does not have a doll, then create it
            if (!players.ContainsKey(sender)) players.Add(sender, RemotePlayer.CreatePlayer());

            // read player data
            players[sender].Read(r);
        });

        Listen(PacketType.SpawnBullet, (sender, r) =>
        {
            byte[] data = r.Bytes(41); // read bullet data
            Bullets.Read(data); // spawn a bullet

            // send bullet data to everyone else
            LobbyController.EachMemberExceptOwnerAnd(sender, member => Networking.SendEvent(member.Id, data, (int)PacketType.SpawnBullet));
        });

        Listen(PacketType.DamagePlayer, (sender, r) =>
        {
            Networking.CurrentOwner = r.Id();

            if (Networking.CurrentOwner == SteamClient.SteamId)
                // damage dealt to the host
                NewMovement.Instance.GetHurt((int)r.Float(), false, 0f);
            else
                // damage was deal to a player, so redirect the packet to him
                Networking.SendEvent(Networking.CurrentOwner, r.Bytes(4), (int)PacketType.DamagePlayer);
        });
    }

    public override void Update()
    {
        // write snapshot
        byte[] data = Writer.Write(w => entities.ForEach(entity =>
        {
            w.Int(entity.Id);
            w.Id(entity.Owner);
            w.Int((int)entity.Type);

            entity.Write(w);
        }));

        // send snapshot
        LobbyController.EachMemberExceptOwner(member => Networking.SendEvent(member.Id, data, (int)PacketType.Snapshot));

        // read incoming packets
        UpdateListeners();
    }
}