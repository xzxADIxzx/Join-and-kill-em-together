namespace Jaket.Net.Types;

using Steamworks;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI;
using Jaket.UI.Dialogs;
using Jaket.World;

/// <summary>
/// Local player that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote player.
/// </summary>
public class LocalPlayer : Entity
{
    private NewMovement nm => NewMovement.Instance;
    private FistControl fc => FistControl.Instance;
    private GameObject cw => GunControl.Instance.currentWeapon;

    /// <summary> Local player's team, changes through the players list. </summary>
    public Team Team;
    /// <summary> Component that reproduces the voice of the local player, not his teammates. </summary>
    public AudioSource Voice;

    /// <summary> Whether the player parried a projectile or just punched. </summary>
    public bool Parried;
    /// <summary> Hook position. Will be zero if the hook is not currently in use. </summary>
    public Vector3 Hook;
    /// <summary> The entity of the item the player is currently holding in their hands. </summary>
    public Item HeldItem;
    /// <summary> Entity that the player pulls to himself with a hook. </summary>
    public Entity Pulled;

    /// <summary> Index of the current weapon in the global list. </summary>
    private byte weapon;

    private void Awake()
    {
        Id = Tools.AccId;
        Type = EntityType.Player;

        Voice = gameObject.AddComponent<AudioSource>(); // add a 2D audio source that will be heard from everywhere

        Events.OnLoaded += () => Events.Post(UpdateWeapons);
        Events.OnWeaponChanged += () => Events.Post(UpdateWeapons);
    }

    private void Update()
    {
        if (HeldItem == null || HeldItem.IsOwner) return;
        HeldItem = null;

        fc.currentPunch.ForceThrow();
        if (fc.currentPunch.holding) fc.currentPunch.PlaceHeldObject(new ItemPlaceZone[0], null);
    }

    #region special

    /// <summary> Synchronizes the style of the local player. </summary>
    public void SyncStyle() => Networking.Send(PacketType.Style, w =>
    {
        w.Id(Id);
        if (cw?.GetComponentInChildren<GunColorGetter>()?.TryGetComponent<Renderer>(out var renderer) ?? false)
        {
            bool custom = renderer.material.name.Contains("Custom");
            w.Bool(custom);

            if (custom) UIB.Properties(renderer, block =>
            {
                w.Color(block.GetColor("_CustomColor1"));
                w.Color(block.GetColor("_CustomColor2"));
                w.Color(block.GetColor("_CustomColor3"));
            });
            else w.Inc(12);
        }
        else w.Inc(13);
    }, size: 21);

    /// <summary> Caches different things related to weapons and paints hands. </summary>
    public void UpdateWeapons()
    {
        weapon = Weapons.Type();
        if (LobbyController.Online) SyncStyle();

        // according to the lore, the player plays for V3, so we need to paint the hand
        var punch = fc.transform.Find("Arm Blue(Clone)");
        if (punch) punch.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();

        var right = cw?.transform.GetChild(0).Find("RightArm");
        if (right) right.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();

        var knuckle = fc.transform.Find("Arm Red(Clone)");
        if (knuckle) knuckle.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture(false);
    }

    #endregion
    #region entity

    public override void Write(Writer w)
    {
        w.Vector(nm.transform.position);
        w.Float(nm.transform.eulerAngles.y);
        w.Float(135f - Mathf.Clamp(CameraController.Instance.rotationX, -40f, 80f));

        w.Byte((byte)nm.hp);
        w.Byte((byte)Mathf.Floor(WeaponCharges.Instance.raicharge * 2.5f));
        w.Enum(Team);
        w.Byte(weapon);
        w.Byte(Movement.Instance.Emoji);
        w.Byte(Movement.Instance.Rps);

        w.Bools(nm.walking, nm.sliding, nm.slamForce > 0f && !nm.gc.onGround, nm.boost && !nm.sliding,
                nm.ridingRocket != null, !nm.gc.onGround, fc.shopping, Chat.Shown);

        w.Bool(Hook != Vector3.zero && HookArm.Instance.enabled);
        w.Vector(Hook);
        w.Id(Pulled?.Id ?? 0L);
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) { }

    public override void Damage(Reader r)
    {
        var team = r.Enum<Team>();
        if (!nm.dead && !team.Ally()) // no need to deal damage if an ally hits you
        {
            byte type = r.Byte();
            nm.GetHurt(Mathf.CeilToInt(r.Float() * (Bullets.Types[type] == "drill" ? 1f : 5f)), false, 0f, r.Bool());

            if (nm.dead) LobbyController.Lobby?.SendChatString("#/s" + (byte)team);
        }
    }

    // why would you need this?
    public override void Kill() { }

    #endregion
}
