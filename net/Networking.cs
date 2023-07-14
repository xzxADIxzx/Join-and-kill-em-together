namespace Jaket.Net;

using System.Collections.Generic;
using System;
using Steamworks;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EntityTypes;
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
            entities.Add(RemotePlayer.CreatePlayer());

            // send current scene name to the player
            SendEvent(friend.Id, Writer.Write(w => w.String(SceneHelper.CurrentScene)), 2);
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) =>
        {
            // send notification
            lobby.SendChatString("<system><color=red>Player " + friend.Name + " left!</color>");

            // destroy remote player doll
            if (players.TryGetValue(friend.Id, out var player))
            {
                entities.Remove(player); // TODO remove because it breaks id logic
                players.Remove(friend.Id);

                GameObject.Destroy(player.gameObject);
            }
        };

        // create a local player to sync the player data
        LocalPlayer = Utils.Object("Local Player", Plugin.Instance.transform).AddComponent<LocalPlayer>();

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
                if (eventType == 2) SceneHelper.LoadScene(r.String());
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
        byte[] data = Writer.Write(w =>
        {
            foreach (var entity in entities)
            {
                // write entity
                w.Int(entity.Id);
                w.Id(entity.Owner);
                w.Int((int)entity.Type);

                entity.Write(w);
            }
        });

        // send snapshot
        LobbyController.EachMemberExceptOwner(member => SendSnapshot(member.Id, data));

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
                    byte[] data = r.Bytes(41); // read bullet data
                    Bullets.Read(data); // spawn a bullet

                    // send bullet data to everyone else
                    LobbyController.EachMemberExceptOwnerAnd(id, member => SendEvent(member.Id, data, 1));
                    break;
                case 1:
                    CurrentOwner = r.Id();

                    if (CurrentOwner == SteamClient.SteamId)
                        NewMovement.Instance.GetHurt((int)r.Float(), false, 0f);
                    else
                        SendEvent(CurrentOwner, r.Bytes(4), 1);
                    break;
            }
        });
    }

    public void ClientUpdate()
    {
        // send player data
        byte[] data = Writer.Write(LocalPlayer.Write);
        SendSnapshot(LobbyController.Lobby.Value.Owner.Id, data);

        // read incoming packets
        ReadPackets((lobbyOwner, r) =>
        {
            while (r.Position < r.Length)
            {
                // read entity
                int id = r.Int();
                CurrentOwner = r.Id();
                int type = r.Int();

                // if the entity is not in the list, add a new one with the given type or local if available
                if (entities.Count <= id) entities.Add(CurrentOwner == SteamClient.SteamId ? LocalPlayer : Entities.Get((EntityType)type));

                entities[id].Read(r);
            }
        }, (lobbyOwner, r, eventType) =>
        {
            if (eventType == 0) Bullets.Read(r);
            if (eventType == 1) NewMovement.Instance.GetHurt((int)r.Float(), false, 0f);
        });
    }

    #region communication

    /// <summary> Sends data to the snapshot channel. </summary>
    public static void SendSnapshot(SteamId id, byte[] data) => SteamNetworking.SendP2PPacket(id, data, nChannel: 0, sendType: P2PSend.Unreliable);

    /// <summary> Sends data to the event channel. </summary>
    public static void SendEvent(SteamId id, byte[] data, int eventType) => SteamNetworking.SendP2PPacket(id, data, nChannel: 1 + eventType);

    /// <summary> Sends data to the host via the event channel. </summary>
    public static void SendEvent2Host(byte[] data, int eventType) => SendEvent(LobbyController.Lobby.Value.Owner.Id, data, eventType);

    /// <summary> Reads data from the snapshot and event channel. </summary>
    public static void ReadPackets(Action<SteamId, Reader> snapshotReader, Action<SteamId, Reader, int> eventReader)
    {
        // read snapshots
        while (SteamNetworking.IsP2PPacketAvailable(0))
        {
            var packet = SteamNetworking.ReadP2PPacket(0);
            if (packet.HasValue) Reader.Read(packet.Value.Data, r => snapshotReader(packet.Value.SteamId, r));
        }

        // read events
        for (int eventType = 0; eventType <= 2; eventType++)
            while (SteamNetworking.IsP2PPacketAvailable(1 + eventType))
            {
                var packet = SteamNetworking.ReadP2PPacket(1 + eventType);
                if (packet.HasValue) Reader.Read(packet.Value.Data, r => eventReader(packet.Value.SteamId, r, eventType));
            }
    }

    #endregion
}