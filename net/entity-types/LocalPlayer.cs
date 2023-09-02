namespace Jaket.Net.EntityTypes;

using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI;
using Jaket.World;

/// <summary>
/// Local player that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote player.
/// </summary>
public class LocalPlayer : Entity
{
    /// <summary> Player team. Changes through the player list. </summary>
    public Team team;

    /// <summary> Whether the player parried a projectile or just punched. </summary>
    public bool parried;

    /// <summary> Hook position. Will be zero if the hook is not currently in use. </summary>
    public Vector3 hook;

    /// <summary> Index of the current weapon in the global list. </summary>
    private byte weapon;

    /// <summary> Weapon rendering component, needed to get weapon colors. </summary>
    private Renderer renderer;

    private void Awake()
    {
        Id = SteamClient.SteamId;
        Type = EntityType.Player;

        SceneManager.sceneLoaded += (scene, mode) => Invoke("UpdateWeapon", .01f);
    }

    /// <summary> Caches different things related to weapons and paints hands. </summary>
    public void UpdateWeapon()
    {
        weapon = (byte)Weapons.CurrentIndex();
        renderer = GunControl.Instance.currentWeapon?.GetComponentInChildren<GunColorGetter>()?.GetComponent<Renderer>();

        FistControl.Instance.blueArm.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();
        var rightArm = GunControl.Instance.currentWeapon?.transform.GetChild(0).Find("RightArm");
        if (rightArm != null) rightArm.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();
    }

    /// <summary> Initiates self-destruction of the player. </summary>
    public void SelfDestruct()
    {
        if (!Chat.Instance.Shown) NewMovement.Instance.GetHurt(1000, false, 0f);
    }

    public override void Write(Writer w)
    {
        w.Float(NewMovement.Instance.hp);
        w.Vector(NewMovement.Instance.transform.position);
        w.Float(NewMovement.Instance.transform.eulerAngles.y);
        w.Float(135f - Mathf.Clamp(CameraController.Instance.rotationX, -40f, 80f));

        w.Byte((byte)team);
        w.Byte(weapon);
        w.Byte(Movement.Instance.Emoji);

        w.Bool(NewMovement.Instance.walking);
        w.Bool(NewMovement.Instance.sliding);
        w.Bool(NewMovement.Instance.slamForce > 0f && !NewMovement.Instance.gc.onGround);
        w.Bool(NewMovement.Instance.boost && !NewMovement.Instance.sliding);
        w.Bool(NewMovement.Instance.ridingRocket != null);
        w.Bool(!NewMovement.Instance.gc.onGround);
        w.Bool(Chat.Instance.Shown);

        w.Bool(hook != Vector3.zero && HookArm.Instance.enabled);
        w.Vector(hook);

        if (renderer != null)
        {
            bool custom = renderer.material.name.Contains("Custom");
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
    public override void Read(Reader r) {}

    public override void Damage(Reader r)
    {
        // no need to deal damage if an ally hits you
        if ((Team)r.Byte() == team) return;

        r.Bool(); // skip melee
        r.Vector(); // skip force, huh

        // otherwise, you need to damage the player
        NewMovement.Instance.GetHurt(Mathf.CeilToInt(r.Float() * 5f), false, 0f, r.Bool());
    }
}
