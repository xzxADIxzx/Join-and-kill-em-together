namespace Jaket.Net;

using Steamworks;
using System.IO;

using Jaket.Content;
using Jaket.UI;

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
        Type = EntityType.Player;
        Owner = SteamClient.SteamId.Value;
    }

    public override void Write(BinaryWriter w)
    {
        var player = NewMovement.Instance.transform;

        // health & position
        w.Write((float)NewMovement.Instance.hp);
        w.Write(player.position.x);
        w.Write(player.position.y);
        w.Write(player.position.z);
        w.Write(player.eulerAngles.y);

        // animation
        w.Write(Chat.Shown);
        w.Write(NewMovement.Instance.walking);
        w.Write(NewMovement.Instance.sliding);
        w.Write(Weapons.CurrentWeaponIndex());
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(BinaryReader r) => r.ReadBytes(27); // skip all data
}