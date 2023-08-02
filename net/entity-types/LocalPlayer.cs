namespace Jaket.Net.EntityTypes;

using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary> Index of the current weapon in the global list. </summary>
    private byte weapon;

    /// <summary> Weapon rendering component, needed to get weapon colors. </summary>
    private SkinnedMeshRenderer renderer;

    private void Awake()
    {
        Id = SteamClient.SteamId;
        Type = EntityType.Player;

        SceneManager.sceneLoaded += (scene, mode) => UpdateWeapon();
    }

    public void UpdateWeapon()
    {
        weapon = (byte)Weapons.CurrentIndex();
        renderer = GunControl.Instance.currentWeapon.GetComponentInChildren<GunColorGetter>()?.GetComponent<SkinnedMeshRenderer>();
    }

    public override void Write(Writer w)
    {
        w.Float(NewMovement.Instance.hp);
        w.Vector(NewMovement.Instance.transform.position);
        w.Float(NewMovement.Instance.transform.eulerAngles.y);
        w.Float(135f - Mathf.Clamp(CameraController.Instance.rotationX, -40f, 80f));

        w.Byte((byte)team);
        w.Byte(weapon);

        w.Bool(NewMovement.Instance.walking);
        w.Bool(NewMovement.Instance.sliding);
        w.Bool(!NewMovement.Instance.gc.onGround);
        w.Bool(Chat.Instance.Shown);

        if (renderer != null)
        {
            bool custom = renderer.material.name.Contains("CustomColor");
            w.Bool(custom);

            if (custom)
            {
                w.Color(renderer.material.GetColor("_CustomColor1"));
                w.Color(renderer.material.GetColor("_CustomColor2"));
                w.Color(renderer.material.GetColor("_CustomColor3"));
            }
            else w.Bytes(new byte[12]);
        }
        else w.Bytes(new byte[13]);
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) => r.Bytes(43); // skip all data

    public override void Damage(Reader r)
    {
        // no need to deal damage if an ally hits you
        if ((Team)r.Int() == team) return;

        r.Vector(); // skip force, huh

        // otherwise, you need to damage the player
        NewMovement.Instance.GetHurt(Mathf.CeilToInt(r.Float() * 5f), false, 0f, r.Bool());
    }
}
