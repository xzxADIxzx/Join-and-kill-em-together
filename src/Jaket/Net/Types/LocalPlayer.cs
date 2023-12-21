namespace Jaket.Net.Types;

using Steamworks;
using UnityEngine;

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
    private NewMovement nm => NewMovement.Instance;
    private FistControl fc => FistControl.Instance;

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

    /// <summary> Index of the current weapon in the global list. </summary>
    private byte weapon;
    /// <summary> Weapon rendering component, needed to get weapon colors. </summary>
    private Renderer renderer;

    private void Start() // start is needed to wait for assets to load
    {
        Id = SteamClient.SteamId;
        Type = EntityType.Player;

        Voice = gameObject.AddComponent<AudioSource>(); // add a 2D audio source that will be heard from everywhere
        Voice.outputAudioMixerGroup = DollAssets.Mixer.FindMatchingGroups("Master")[0];

        Events.OnLoaded += () => Events.Post(UpdateWeapons);
        Events.OnWeaponChanged += UpdateWeapons;
    }

    /// <summary> Caches different things related to weapons and paints hands. </summary>
    public void UpdateWeapons()
    {
        weapon = Weapons.Type();
        renderer = GunControl.Instance.currentWeapon?.GetComponentInChildren<GunColorGetter>()?.GetComponent<Renderer>();

        // according to the lore, the player plays for V3, so we need to paint the hand
        fc.blueArm.ToAsset().GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();

        var arm = GunControl.Instance.currentWeapon?.transform.GetChild(0).Find("RightArm");
        if (arm != null) arm.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = DollAssets.HandTexture();
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Float(nm.hp);
        w.Vector(nm.transform.position);
        w.Float(nm.transform.eulerAngles.y);
        w.Float(135f - Mathf.Clamp(CameraController.Instance.rotationX, -40f, 80f));

        w.Byte((byte)Mathf.Floor(WeaponCharges.Instance.raicharge * 2f));
        w.Enum(Team);
        w.Byte(weapon);
        w.Byte(Movement.Instance.Emoji);
        w.Byte(Movement.Instance.Rps);

        w.Bool(nm.walking);
        w.Bool(nm.sliding);
        w.Bool(nm.slamForce > 0f && !nm.gc.onGround);
        w.Bool(nm.boost && !nm.sliding);
        w.Bool(nm.ridingRocket != null);
        w.Bool(!nm.gc.onGround);
        w.Bool(Chat.Shown);
        w.Bool(fc.shopping);

        w.Bool(Hook != Vector3.zero && HookArm.Instance.enabled);
        w.Vector(Hook);

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
            else w.Inc(12);
        }
        else w.Inc(13);

        #region item

        if (HeldItem != null && !HeldItem.IsOwner)
        {
            HeldItem = null;

            fc.currentPunch.ForceThrow();
            if (fc.currentPunch.holding) fc.currentPunch.PlaceHeldObject(new ItemPlaceZone[0], null);
        }

        w.Bool(HeldItem != null);
        if (HeldItem != null)
        {
            w.Id(HeldItem.Id);
            HeldItem.Write(w);
        }

        #endregion
    }

    // there is no point in reading anything, because it is a local player
    public override void Read(Reader r) { }

    public override void Damage(Reader r)
    {
        if (!r.Enum<Team>().Ally()) // no need to deal damage if an ally hits you
        {
            byte type = r.Byte();
            NewMovement.Instance.GetHurt(Mathf.CeilToInt(r.Float() * (Bullets.Types[type] == "drill" ? 2f : 5f)), false, 0f, r.Bool());
        }
    }

    // why would you need this?
    public override void Kill() { }

    #endregion
}
