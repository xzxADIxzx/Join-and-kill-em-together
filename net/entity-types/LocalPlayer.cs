namespace Jaket.Net.EntityTypes;

using Steamworks;
using UnityEngine;

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
        w.Float(NewMovement.Instance.hp);
        w.Vector(NewMovement.Instance.transform.position);
        w.Float(NewMovement.Instance.transform.eulerAngles.y);
        w.Float(-CameraController.Instance.rotationX - 240f);

        w.Byte((byte)team);
        w.Byte((byte)Weapons.CurrentIndex());

        w.Bool(NewMovement.Instance.walking);
        w.Bool(NewMovement.Instance.sliding);
        w.Bool(!NewMovement.Instance.gc.onGround); 
        w.Bool(Chat.Instance.Shown);
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) => r.Bytes(30); // skip all data

    public override void Damage(Reader r)
    {
        // no need to deal damage if an ally hits you
        if ((Team)r.Int() == team) return;

        r.Vector(); // skip force, huh

        // otherwise, you need to damage the player
        NewMovement.Instance.GetHurt(Mathf.CeilToInt(r.Float() * 5f), false, 0f, r.Bool());
    }
}
