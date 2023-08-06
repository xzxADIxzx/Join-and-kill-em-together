namespace Jaket.Net.EndPoints;

using Jaket.Content;
using Jaket.Net.EntityTypes;
using Jaket.IO;

/// <summary> Endpoint of the host/lobby-owner to which clients connect. </summary>
public class Server : Endpoint
{
    public override void Load()
    {
        Listen(PacketType.Snapshot, (sender, r) =>
        {
            // if the player does not have a doll, then create it
            if (!entities.ContainsKey(sender)) entities[sender] = Entities.Get(sender, EntityType.Player);

            // sometimes players disappear for some unknown reason, and sometimes I destroy them myself
            if (entities[sender] == null) entities[sender] = Entities.Get(sender, EntityType.Player);

            // read player data
            entities[sender]?.Read(r);
        });

        Listen(PacketType.SpawnBullet, (sender, r) =>
        {
            byte[] data = r.Bytes(41); // read bullet data
            Bullets.Read(data); // spawn a bullet

            // send bullet data to everyone else
            LobbyController.EachMemberExceptOwnerAnd(sender, member => Networking.Send(member.Id, data, PacketType.SpawnBullet));
        });

        Listen(PacketType.DamageEntity, (sender, r) =>
        {
            entities[r.Id()]?.Damage(r); // damage entity

            r.Position = 0L; // reset position to read all data
            byte[] data = r.Bytes(29); // read damage data

            // send damage data to everyone else
            LobbyController.EachMemberExceptOwnerAnd(sender, member => Networking.Send(member.Id, data, PacketType.DamageEntity));
        });
    }

    public override void Update()
    {
        // write snapshot
        byte[] data = Writer.Write(w => Networking.EachEntity(entity =>
        {
            // when an entity is destroyed via Object.Destroy, the element in the list is replaced with null
            if (entity == null) return;

            w.Id(entity.Id);
            w.Byte((byte)entity.Type);

            entity.Write(w);
        }));

        // send snapshot
        LobbyController.EachMemberExceptOwner(member => Networking.SendSnapshot(member.Id, data));

        // read incoming packets
        UpdateListeners();
    }
}