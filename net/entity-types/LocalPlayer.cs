namespace Jaket.Net;

using Steamworks;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary>
/// Local player that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote player.
/// </summary>
public class LocalPlayer : Entity
{
    /// <summary> Creates a new local player. </summary>
    public static LocalPlayer CreatePlayer() => Plugin.Instance.gameObject.AddComponent<LocalPlayer>();

    public void Awake()
    {
        Type = EntityType.Player;
        Owner = SteamClient.SteamId.Value;
    }

    public override void Write(Writer w)
    {
        // health & position
        w.Float(NewMovement.Instance.hp);
        w.Vector(NewMovement.Instance.transform.position);
        w.Float(NewMovement.Instance.transform.eulerAngles.y);

        // animation
        w.Bool(Chat.Shown);
        w.Bool(NewMovement.Instance.walking);
        w.Bool(NewMovement.Instance.sliding);
        w.Int(Weapons.CurrentIndex());
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) => r.Bytes(27); // skip all data
}