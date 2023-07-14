namespace Jaket.Net.EndPoints;

using Steamworks;
using System.Collections.Generic;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.EntityTypes;

/// <summary> Network connection endpoint that contains listeners for different packet types. </summary>
public abstract class Endpoint
{
    /// <summary> Dictionary of packet types to their listeners. </summary>
    protected Dictionary<PacketType, PacketListener> listeners = new();

    /// <summary> Reference to Networking.Entities. </summary>
    protected List<Entity> entities { get => Networking.entities; }

    /// <summary> Reference to Networking.Players </summary>
    protected Dictionary<SteamId, RemotePlayer> players => Networking.players;

    /// <summary> Loads endpoint listeners and other stuff. </summary>
    public abstract void Load();

    /// <summary> Updates endpoint listeners and other stuff. </summary>
    public abstract void Update();

    /// <summary> Adds a new listener to the endpoint. </summary>
    public void Listen(PacketType type, PacketListener listener) => listeners.Add(type, listener);

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
