namespace Jaket.Net.EndPoints;

using Steamworks;
using System;
using System.Collections.Generic;

using Jaket.Content;
using Jaket.IO;

/// <summary> Network connection endpoint that contains listeners for different packet types. </summary>
public abstract class Endpoint
{
    /// <summary> Dictionary of packet types to their listeners. </summary>
    protected Dictionary<PacketType, PacketListener> listeners = new();

    /// <summary> Reference to Networking.Entities. </summary>
    protected Dictionary<ulong, Entity> entities => Networking.Entities;

    /// <summary> Loads endpoint listeners and other stuff. </summary>
    public abstract void Load();

    /// <summary> Updates endpoint listeners and other stuff. </summary>
    public abstract void Update();

    /// <summary> Adds a new listener to the endpoint. </summary>
    public void Listen(PacketType type, PacketListener listener) => listeners.Add(type, listener);

    /// <summary> Adds a new listener to the endpoint, but without sender. </summary>
    public void Listen(PacketType type, Action<Reader> listener) => listeners.Add(type, (sender, r) => listener(r));

    /// <summary> Adds a new listener to the endpoint that will forward data to clients. </summary>
    public void ListenAndRedirect(PacketType type, Action<Reader> listener) => listeners.Add(type, (sender, r) =>
    {
        listener(r);

        byte[] data = r.AllBytes();
        LobbyController.EachMemberExceptOwnerAnd(sender, member => Networking.Send(member.Id, data, type));
    });

    /// <summary> Reads available packets and pass them to listeners. </summary>
    public void UpdateListeners()
    {
        // go through the keys of the dictionary, because listeners may be missing for some types of packets
        foreach (var packetType in listeners.Keys)
        {
            // read each available packet
            while (SteamNetworking.IsP2PPacketAvailable((int)packetType))
            {
                var packet = SteamNetworking.ReadP2PPacket((int)packetType);
                if (packet.HasValue) Reader.Read(packet.Value.Data, r => listeners[packetType](packet.Value.SteamId, r));
            }
        }
    }

    /// <summary> Packet listener that accepts the sender of the packet and the packet itself. </summary>
    public delegate void PacketListener(SteamId sender, Reader r);
}
