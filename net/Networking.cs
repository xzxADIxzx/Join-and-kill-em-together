namespace Jaket.Net;

using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;

using Jaket.UI;

public class Networking : MonoBehaviour
{
    public const int SNAPSHOTS_PER_SECOND = 20;
    public const float SNAPSHOTS_SPACING = 1f / SNAPSHOTS_PER_SECOND;

    public static List<Entity> entities = new();
    public static Dictionary<SteamId, RemotePlayer> players = new();

    public static LocalPlayer LocalPlayer;

    public static void Load()
    {
        SteamMatchmaking.OnChatMessage += (lobby, friend, message) =>
        {
            if (message.StartsWith("<system>") && friend.Id == lobby.Owner.Id)
                Chat.Received("Lobby", message.Substring("<system>".Length));
            else
                Chat.Received(friend.Name, message);
        };
        SteamFriends.OnGameLobbyJoinRequested += LobbyController.JoinLobby;

        SteamMatchmaking.OnLobbyEntered += lobby =>
        {
            entities.Clear();
            players.Clear();

            if (LobbyController.IsOwner)
                // the lobby has just been created, so just add the local player to the list of entities
                entities.Add(LocalPlayer);
            else
                // establishing a connection with the owner of the lobby
                SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);
        };

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) =>
        {
            // if you are not the owner of the lobby, then you do not need to do anything
            if (!LobbyController.IsOwner) return;

            // send notification
            lobby.SendChatString("<system><color=#00FF00>Player " + friend.Name + " joined!</color>");

            // confirm the connection with the player
            SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);

            // create a new remote player doll
            var player = RemotePlayer.CreatePlayer();
            player.Owner = friend.Id.Value; // this is necessary so that the player does not see his own model

            entities.Add(player);
            players.Add(friend.Id, player);
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) => lobby.SendChatString("<system><color=red>Player " + friend.Name + " left!</color>");

        // create a local player to sync the player data
        LocalPlayer = LocalPlayer.CreatePlayer();

        // create an object to update the network logic
        Utils.Object("Networking", Plugin.Instance.transform).AddComponent<Networking>();
    }

    public void Awake()
    {
        InvokeRepeating("NetworkUpdate", 0f, SNAPSHOTS_SPACING);
    }

    public void NetworkUpdate()
    {
        if (LobbyController.Lobby == null) return;

        if (LobbyController.IsOwner)
            ServerUpdate();
        else
            ClientUpdate();
    }

    public void ServerUpdate()
    {
        // write snapshot
        MemoryStream stream = new MemoryStream();
        using (var w = new BinaryWriter(stream))
        {
            foreach (var entity in entities)
            {
                // write entity
                w.Write(entity.Id);
                w.Write(entity.Owner);
                w.Write((int)entity.Type);

                entity.Write(w);
            }
        }
        byte[] data = stream.ToArray();

        // send snapshot
        foreach (var member in LobbyController.Lobby?.Members)
        {
            // no need to send packets to yourself
            if (member.Id == SteamClient.SteamId) continue;

            // send snapshot to the player
            SteamNetworking.SendP2PPacket(member.Id, data, sendType: P2PSend.Unreliable);
        }

        // read incoming packets
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            var packet = SteamNetworking.ReadP2PPacket();
            if (!packet.HasValue) continue; // how?

            if (players.ContainsKey(packet.Value.SteamId))
            {
                players.TryGetValue(packet.Value.SteamId, out var player);
                using (var r = new BinaryReader(new MemoryStream(packet.Value.Data))) player.Read(r); // read player data
            }
            else Debug.LogError("Couldn't find RemotePlayer for SteamId " + packet.Value.SteamId);
        }
    }

    public void ClientUpdate()
    {
        // send player data
        MemoryStream stream = new MemoryStream();
        using (var w = new BinaryWriter(stream)) LocalPlayer.Write(w);

        SteamNetworking.SendP2PPacket(LobbyController.Lobby.Value.Owner.Id, stream.ToArray(), sendType: P2PSend.Unreliable);

        // read incoming packets
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            var packet = SteamNetworking.ReadP2PPacket();
            if (!packet.HasValue) continue; // how?

            // read snaphot
            stream = new MemoryStream(packet.Value.Data);
            using (var r = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    // read entity
                    int id = r.ReadInt32();
                    ulong owner = r.ReadUInt64();
                    int type = r.ReadInt32();

                    // if the entity is not in the list, add a new one with the given type
                    if (entities.Count <= id) entities.Add(owner == SteamClient.SteamId ? LocalPlayer : Entities.Get((Entities.Type)type));

                    entities[id].Read(r);
                }
            }
        }
    }
}