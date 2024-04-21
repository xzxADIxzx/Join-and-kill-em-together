namespace Jaket.Net.Endpoints;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net.Types;

/// <summary> Network connection endpoint that contains listeners for different packet types. </summary>
public abstract class Endpoint
{
    protected Dictionary<uint, Entity> entities => Networking.Entities;
    protected LocalPlayer localPlayer => Networking.LocalPlayer;

    /// <summary> List of packet listeners by packet types. </summary>
    protected Dictionary<PacketType, PacketListener> listeners = new();

    /// <summary> Loads endpoint listeners and other stuff. </summary>
    public abstract void Load();
    /// <summary> Updates various stuff in the endpoint. </summary>
    public abstract void Update();
    /// <summary> Closes the connection to the endpoint. </summary>
    public abstract void Close();

    /// <summary> Adds a new listener to the endpoint. </summary>
    public void Listen(PacketType type, PacketListener listener) => listeners.Add(type, listener);
    /// <summary> Adds a new listener to the endpoint, but without sender. </summary>
    public void Listen(PacketType type, Action<Reader> listener) => listeners.Add(type, (con, sender, r) => listener(r));

    /// <summary> Adds a new listener to the endpoint that will forward data to clients. </summary>
    public void ListenAndRedirect(PacketType type, Action<Reader> listener) => listeners.Add(type, (con, sender, r) =>
    {
        listener(r);
        Redirect(r, con);
    });
    /// <summary> Forwards data to clients. </summary>
    public void Redirect(Reader data, Connection ignore) => Networking.EachConnection(con =>
    {
        if (con != ignore) Tools.Send(con, data.mem, data.Length);
    });

    /// <summary> Handles the packet and calls the corresponding listener. </summary>
    public void Handle(Connection con, SteamId sender, Reader r)
    {
        var type = r.Enum<PacketType>(); // if the client hasn't downloaded the level yet, then it only needs a packet with the level name
        if (Networking.Loading && type != PacketType.LoadLevel) return;

        // find the required listener and transfer control to it, all it has to do is read the payload
        if (listeners.TryGetValue(type, out var listener)) listener(con, sender, r);
        Stats.Read += r.Length;
    }
    /// <summary> Handles the packet from unmanaged memory. </summary>
    public void Handle(Connection con, SteamId sender, IntPtr data, int size) => Reader.Read(data, size, r => Handle(con, sender, r));

    /// <summary> Packet listener that accepts the sender of the packet and the packet itself. </summary>
    public delegate void PacketListener(Connection con, SteamId sender, Reader r);
}
