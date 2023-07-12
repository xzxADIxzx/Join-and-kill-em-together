namespace Jaket.Net;

using System.Collections.Generic;
using System.IO;
using System;
using Steamworks;
using UnityEngine;

using Jaket.UI;

public class Networking : MonoBehaviour
{
    public const int SNAPSHOTS_PER_SECOND = 20;
    public const float SNAPSHOTS_SPACING = 1f / SNAPSHOTS_PER_SECOND;

    public static List<Entity> entities = new();
    public static Dictionary<SteamId, RemotePlayer> players = new();

    /// <summary> Local player representation. </summary>
    public static LocalPlayer LocalPlayer;
    /// <summary> Owner of the entity currently being processed. </summary>
    public static SteamId CurrentOwner;
    /// <summary> Whether a scene is loading right now. </summary>
    public static bool Loading;

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
            {
                // establishing a connection with the owner of the lobby
                SteamNetworking.AcceptP2PSessionWithUser(lobby.Owner.Id);

                // prevent objects from loading before the scene is loaded
                Loading = true;
            }
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
            CurrentOwner = friend.Id; // this is necessary so that the player does not see his own model
            var player = RemotePlayer.CreatePlayer();

            entities.Add(player);
            players.Add(friend.Id, player);

            // send current scene name to the player
            SendEvent(friend.Id, Write(w => w.Write(SceneHelper.CurrentScene)), 2);
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) => lobby.SendChatString("<system><color=red>Player " + friend.Name + " left!</color>");

        // create a local player to sync the player data
        CurrentOwner = SteamClient.SteamId;
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

        if (Loading)
        {
            ReadPackets((id, r) => { }, (id, r, eventType) =>
            {
                if (eventType == 2) SceneHelper.LoadScene(r.ReadString());
            });
            return;
        }

        if (LobbyController.IsOwner)
            ServerUpdate();
        else
            ClientUpdate();
    }

    public void ServerUpdate()
    {
        // write snapshot
        byte[] data = Write(w =>
        {
            foreach (var entity in entities)
            {
                // write entity
                w.Write(entity.Id);
                w.Write(entity.Owner);
                w.Write((int)entity.Type);

                entity.Write(w);
            }
        });

        // send snapshot
        foreach (var member in LobbyController.Lobby?.Members)
        {
            // no need to send packets to yourself
            if (member.Id == SteamClient.SteamId) continue;

            // send snapshot to the player
            SendSnapshot(member.Id, data);
        }

        // read incoming packets
        ReadPackets((id, r) =>
        {
            if (players.ContainsKey(id))
            {
                players.TryGetValue(id, out var player);
                player.Read(r); // read player data
            }
            else Debug.LogError("Couldn't find RemotePlayer for SteamId " + id); // TODO create remote player
        }, (id, r, eventType) =>
        {
            switch (eventType) // TODO look super bad
            {
                case 0:
                    byte[] data = r.ReadBytes(41); // read bullet data
                    Weapons.InstantinateBullet(data); // spawn a bullet

                    foreach (var member in LobbyController.Lobby?.Members) // send bullet data to everyone else
                        if (member.Id != SteamClient.SteamId && member.Id != id) SendEvent(member.Id, data, 1);
                    break;
                case 1:
                    CurrentOwner = r.ReadUInt64();

                    if (CurrentOwner == SteamClient.SteamId)
                        NewMovement.Instance.GetHurt((int)r.ReadSingle(), false, 0f);
                    else
                        SendEvent(CurrentOwner, r.ReadBytes(4), 1);
                    break;
            }
        });
    }

    public void ClientUpdate()
    {
        // send player data
        byte[] data = Write(LocalPlayer.Write);
        SendSnapshot(LobbyController.Lobby.Value.Owner.Id, data);

        // read incoming packets
        ReadPackets((lobbyOwner, r) =>
        {
            while (r.BaseStream.Position < r.BaseStream.Length)
            {
                // read entity
                int id = r.ReadInt32();
                CurrentOwner = r.ReadUInt64();
                int type = r.ReadInt32();

                // if the entity is not in the list, add a new one with the given type or local if available
                if (entities.Count <= id) entities.Add(CurrentOwner == SteamClient.SteamId ? LocalPlayer : Entities.Get((Entities.Type)type));

                entities[id].Read(r);
            }
        }, (lobbyOwner, r, eventType) =>
        {
            if (eventType == 0) Weapons.InstantinateBullet(r);
            if (eventType == 1) NewMovement.Instance.GetHurt((int)r.ReadSingle(), false, 0f);
        });
    }

    #region io

    /// <summary> Writes data to return byte array via BinaryWriter. </summary>
    public static byte[] Write(Action<BinaryWriter> writer)
    {
        MemoryStream stream = new MemoryStream();
        using (var w = new BinaryWriter(stream)) writer.Invoke(w);

        return stream.ToArray();
    }

    /// <summary> Reads data from the given byte array via BinaryReader. </summary>
    public static void Read(byte[] data, Action<BinaryReader> reader)
    {
        MemoryStream stream = new MemoryStream(data);
        using (var r = new BinaryReader(stream)) reader.Invoke(r);
    }

    #endregion
    #region communication

    /// <summary> Sends data to the snapshot channel. </summary>
    public static void SendSnapshot(SteamId id, byte[] data) => SteamNetworking.SendP2PPacket(id, data, nChannel: 0, sendType: P2PSend.Unreliable);

    /// <summary> Sends data to the event channel. </summary>
    public static void SendEvent(SteamId id, byte[] data, int eventType) => SteamNetworking.SendP2PPacket(id, data, nChannel: 1 + eventType);

    /// <summary> Sends data to the host via the event channel. </summary>
    public static void SendEvent2Host(byte[] data, int eventType) => SendEvent(LobbyController.Lobby.Value.Owner.Id, data, eventType);

    /// <summary> Reads data from the snapshot and event channel. </summary>
    public static void ReadPackets(Action<SteamId, BinaryReader> snapshotReader, Action<SteamId, BinaryReader, int> eventReader)
    {
        // read snapshots
        while (SteamNetworking.IsP2PPacketAvailable(0))
        {
            var packet = SteamNetworking.ReadP2PPacket(0);
            if (packet.HasValue) Read(packet.Value.Data, r => snapshotReader.Invoke(packet.Value.SteamId, r));
        }

        // read events
        for (int eventType = 0; eventType <= 2; eventType++)
            while (SteamNetworking.IsP2PPacketAvailable(1 + eventType))
            {
                var packet = SteamNetworking.ReadP2PPacket(1 + eventType);
                if (packet.HasValue) Read(packet.Value.Data, r => eventReader.Invoke(packet.Value.SteamId, r, eventType));
            }
    }

    #endregion
}