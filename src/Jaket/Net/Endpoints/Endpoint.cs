namespace Jaket.Net.Endpoints;

using Steamworks.Data;

using Jaket.Content;
using Jaket.IO;

/// <summary> Endpoint of a network connection that processes incoming data via packet handlers. </summary>
public abstract class Endpoint
{
    /// <summary> Handlers of incoming data packets. </summary>
    protected PacketHandler[] handlers = new PacketHandler[32];
    /// <summary> Counter showing the pool that will be included in the next snapshot. </summary>
    protected int pool;

    /// <summary> Creates handlers to process incoming data. </summary>
    public abstract void Create();
    /// <summary> Updates various stuff in the endpoint. </summary>
    public abstract void Update();
    /// <summary> Closes the connection to the endpoint. </summary>
    public abstract void Close();

    /// <summary> Adds a new handler to the endpoint. </summary>
    public void Listen(PacketType type, PacketHandler listener) => handlers[(int)type] = listener;

    /// <summary> Adds a new handler to the endpoint. </summary>
    public void Listen(PacketType type, Cons<Reader> listener) => handlers[(int)type] = (con, sender, data, size) => listener(data);

    /// <summary> Handles the incoming data packet. </summary>
    public void Handle(Connection con, uint sender, Ptr data, int size)
    {
        Reader r = new(data);
        var type = r.PacketType();

        if (Networking.Loading && type != PacketType.Level && type != PacketType.ImageChunk) return;

        handlers[(int)type](con, sender, r, size);
        Stats.ReadBs += size;
    }

    /// <summary> Forwards the packet to all of the clients. </summary>
    public void Redirect(Reader data, int size, Connection ignore) => Networking.Connections.Each(c => c != ignore, c => Networking.Send(c, data.Memory, size));

    /// <summary> Verifies the identifier and then forwards. </summary>
    public bool Redirect(Reader data, int size, Connection ignore, uint sender)
    {
        var valid = data.Id() == sender;
        if (valid) Redirect(data, size, ignore);
        else
        {
            Administration.Ban(sender);
            Log.Warning($"[SERVER] {sender} was blocked: falsification of identifier");
        }
        return valid;
    }

    /// <summary> Backbone of the entire project networking. </summary>
    public delegate void PacketHandler(Connection con, uint sender, Reader data, int size);
}
