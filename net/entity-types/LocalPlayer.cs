namespace Jaket.Net;

using System.IO;

/// <summary>
/// Local player that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote player
/// </summary>
public class LocalPlayer : Entity
{
    /// <summary> Creates a new local player. </summary>
    public static LocalPlayer CreatePlayer() => Plugin.Instance.gameObject.AddComponent<LocalPlayer>();

    public void Awake()
    {
        Type = Entities.Type.player;
    }

    public override void Write(BinaryWriter w)
    {
        w.Write((int)Type);

        var player = NewMovement.Instance.transform;
        w.Write(player.position.x);
        w.Write(player.position.y);
        w.Write(player.position.z);
        w.Write(player.eulerAngles.y);
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(BinaryReader r) {}
}