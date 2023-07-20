namespace Jaket.Net.EntityTypes;

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
    /// <summary> Player team. Changes through the player list. </summary>
    public Team team;

    public void Awake()
    {
        Owner = SteamClient.SteamId.Value;
        Type = EntityType.Player;
    }

    public override void Write(Writer w)
    {
        // health & position
        w.Float(NewMovement.Instance.hp);
        w.Vector(NewMovement.Instance.transform.position);
        w.Float(NewMovement.Instance.transform.eulerAngles.y);
        w.Float(-CameraController.Instance.rotationX);

        // animation
        w.Bool(Chat.Shown);
        w.Bool(NewMovement.Instance.walking);
        w.Bool(NewMovement.Instance.sliding);
        w.Int((int)team);
        w.Int(Weapons.CurrentIndex());
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) => r.Bytes(31); // skip all data
}
