namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI;
using Jaket.UI.Elements;

/// <summary>
/// Remote player that exists both on the local machine and on the remote one.
/// Responsible for the visual part of the player, i.e. model and animation, and for logic, i.e. health and teams.
/// </summary>
public class RemotePlayer : Entity
{
    /// <summary> Position of the player, the rotation of its body and head and the position of the hook. </summary>
    private FloatLerp x, y, z, bodyRotation, headRotation, hookX, hookY, hookZ;
    /// <summary> Transforms of the head, the hand holding a weapon and other stuff. </summary>
    private Transform head, hand, hook, hookRoot, rocket, throne;
    /// <summary> Animator states that affect which animation will be played. </summary>
    private bool walking, sliding, falling, dashing, wasDashing, riding, wasRiding, inAir, wasInAir, usingHook, wasUsingHook, shopping, wasShopping;

    /// <summary> Whether the player is currently invincible. </summary>
    public bool Invincible => dashing || Health == 0;
    /// <summary> Position in which the player holds an item. </summary>
    public Vector3 HoldPosition => usingHook ? hook.position : hookRoot.position;
    /// <summary> Entity that the player pulls to himself with a hook. </summary>
    public EntityProv<Entity> Pulled = new();

    /// <summary> Materials of the wings and skateboard. </summary>
    private Material wingMaterial, skateMaterial;
    /// <summary> Trail of the wings. </summary>
    private TrailRenderer wingTrail;
    /// <summary> Winch of the hook. </summary>
    private LineRenderer hookWinch;

    /// <summary> Whether the player use custom weapon colors. </summary>
    private bool customColors;
    /// <summary> Custom weapon colors. </summary>
    private Color32 color1, color2, color3;
    /// <summary> Slide and fall particle transforms. </summary>
    private Transform slideParticle, fallParticle;

    /// <summary> Health may not match the real one due to byte limitations. </summary>
    public byte Health;
    /// <summary> Player's railgun charge. From 0 to 10. </summary>
    public byte RailCharge;

    /// <summary> Player team needed for PvP mechanics. </summary>
    public Team Team, LastTeam = (Team)0xFF;
    /// <summary> Weapon id needed only for visual. </summary>
    public byte Weapon, LastWeapon = 0xFF;
    /// <summary> Emoji id needed only for fun. </summary>
    public byte Emoji, LastEmoji = 0xFF, Rps;

    /// <summary> Machine component of the player doll. </summary>
    public Machine Machine;
    /// <summary> Component responsible for playing Sam's voice. </summary>
    public AudioSource Voice;
    /// <summary> Whether the player is typing a message. </summary>
    public bool Typing;

    /// <summary> Header displaying nickname and health. </summary>
    public PlayerHeader Header;
    /// <summary> Last pointer created by the player. </summary>
    public Pointer Pointer;

    private void Awake()
    {
        Init(null, () => true);

        x = new(); y = new(); z = new();
        bodyRotation = new();
        headRotation = new();
        hookX = new(); hookY = new(); hookZ = new();

        var v3 = transform.GetChild(0);
        var rig = v3.GetChild(1);

        head = rig.GetChild(6).GetChild(10).GetChild(0);
        hand = Tools.Create("Weapons", rig.GetChild(6).GetChild(5).GetChild(0).GetChild(0)).transform;
        hook = rig.GetChild(1);
        hookRoot = rig.GetChild(6).GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        rocket = rig.GetChild(4).GetChild(1);
        throne = rig.GetChild(7);

        wingMaterial = v3.GetChild(4).GetComponent<Renderer>().materials[1];
        skateMaterial = v3.GetChild(3).GetComponent<Renderer>().materials[0];
        wingTrail = GetComponentInChildren<TrailRenderer>();
        hookWinch = GetComponentInChildren<LineRenderer>(true);
        Machine = GetComponent<Machine>();
        Voice = GetComponent<AudioSource>();

        EnemyId.weakPoint = head.gameObject;
        hookWinch.material = HookArm.Instance.GetComponent<LineRenderer>().material;

        // on some levels there are no weapons at all
        if (GunSetter.Instance == null) return;

        var prefab = Bullets.Prefabs[Bullets.CType("RL PRI")].GetComponentInChildren<SpriteRenderer>().gameObject;
        var flash = Instantiate(prefab, rocket).transform;

        flash.localPosition = new();
        flash.localScale = new(.02f, .02f, .02f);
    }

    private void Start()
    {
        Header = new(Id, transform);

        // idols can target players, which is undesirable
        int index = EnemyTracker.Instance.enemies.IndexOf(EnemyId);
        if (index != -1)
        {
            EnemyTracker.Instance.enemies.RemoveAt(index);
            EnemyTracker.Instance.enemyRanks.RemoveAt(index);
        }
    }

    private void Update()
    {
        Header.Update(Health, Typing);
        if (Animator == null) // the player is dead
        {
            if (Health != 0) Destroy(gameObject); // the player has respawned, the doll needs to be recreated
            return;
        }
        else if (Health == 0) Machine.GoLimp();

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate) - (sliding ? .3f : 1.5f), z.Get(LastUpdate));
        transform.eulerAngles = new(0f, bodyRotation.GetAngel(LastUpdate), 0f);
        head.localEulerAngles = new(Emoji == 8 ? -20f : headRotation.Get(LastUpdate), 0f, 0f);

        #region changes & triggers

        Machine.health = Health;
        gameObject.tag = Team.Ally() ? "Untagged" : "Enemy"; // toggle friendly fire

        if (LastTeam != Team)
        {
            wingMaterial.mainTexture = skateMaterial.mainTexture = DollAssets.WingTextures[(int)(LastTeam = Team)];
            wingTrail.startColor = Team.Color() with { a = .5f };

            transform.GetChild(0).GetChild(0).gameObject.SetActive(Team == Team.Pink); // pink team has cat ears
            Events.OnTeamChanged.Fire();
        }
        if (LastWeapon != Weapon)
        {
            foreach (Transform child in hand) Destroy(child.gameObject);
            if ((LastWeapon = Weapon) != 0xFF)
            {
                Weapons.Instantiate(Weapon, hand);
                WeaponsOffsets.Apply(Weapon, hand);
                UpdateStyle();
            }
        }
        if (LastEmoji != Emoji)
        {
            Animator.SetTrigger("Show Emoji");
            Animator.SetInteger("Emoji", LastEmoji = Emoji);
            Animator.SetInteger("Rps", Rps);

            // toggle the visibility of the throne
            throne.gameObject.SetActive(Emoji == 6);

            // recreate the weapon if the animation is over
            if (Emoji == 0xFF) LastWeapon = 0xFF;
            // or destroy it if the animation has started
            else foreach (Transform child in hand) Destroy(child.gameObject);
        }

        if (wasDashing != dashing && (wasDashing = dashing)) Animator.SetTrigger("Dash");
        if (wasInAir != inAir && (wasInAir = inAir)) Animator.SetTrigger("Jump");
        if (wasShopping != shopping && (wasShopping = shopping)) Animator.SetTrigger("Open Shop");

        if (wasRiding != riding)
        {
            if (wasRiding = riding) Animator.SetTrigger("Ride");
            rocket.gameObject.SetActive(riding);
        }
        if (wasUsingHook != usingHook)
        {
            if (wasUsingHook = usingHook) Animator.SetTrigger("Throw Hook");

            hook.gameObject.SetActive(usingHook);
            hook.position = new(hookX.Target, hookY.Target, hookZ.Target);
        }

        #endregion

        Animator.SetBool("Walking", walking);
        Animator.SetBool("Sliding", sliding);
        Animator.SetBool("Dashing", dashing);
        Animator.SetBool("Riding", riding);
        Animator.SetBool("InAir", inAir);
        Animator.SetBool("UsingHook", usingHook);
        Animator.SetBool("Shopping", shopping);

        if (sliding && slideParticle == null)
        {
            slideParticle = Instantiate(NewMovement.Instance.slideParticle, transform).transform;
            slideParticle.localPosition = new(0f, 0f, 2f);
            slideParticle.localEulerAngles = new(0f, 180f, 0f);
            slideParticle.localScale = new(1f, 1f, .5f);
        }
        else if (!sliding && slideParticle != null) Destroy(slideParticle.gameObject);

        if (falling && fallParticle == null)
        {
            fallParticle = Instantiate(NewMovement.Instance.fallParticle, transform).transform;
            fallParticle.localPosition = new(0f, 5f, 0f);
            fallParticle.localEulerAngles = new(90f, 0f, 0f);
            fallParticle.localScale = new(1f, .5f, 1f);
        }
        else if (!falling && fallParticle != null) Destroy(fallParticle.gameObject);
    }

    private void LateUpdate()
    {
        // everything related to the hook is in LateUpdate, because it is a child of the player's doll and moves with it
        hook.position = new(hookX.Get(LastUpdate), hookY.Get(LastUpdate), hookZ.Get(LastUpdate));
        hook.LookAt(transform);
        hook.Rotate(Vector3.up * 180f, Space.Self);

        hookWinch.SetPosition(0, hookRoot.position);
        hookWinch.SetPosition(1, hook.position);

        // pull the entity caught by the hook
        if (!LobbyController.IsOwner) return;

        var pl = Pulled.Value;
        if (pl && pl.EnemyId && pl.Rb)
        {
            if (pl.Rb.isKinematic) pl.EnemyId.gce.ForceOff();
            pl.Rb.velocity = (transform.position - pl.transform.position).normalized * 60f;
        }
    }

    private void UpdateStyle()
    {
        foreach (var getter in hand.GetComponentsInChildren<GunColorGetter>())
        {
            var renderer = getter.GetComponent<Renderer>();
            if (customColors)
            {
                renderer.materials = getter.coloredMaterials;
                UIB.Properties(renderer, block =>
                {
                    block.SetColor("_CustomColor1", color1);
                    block.SetColor("_CustomColor2", color2);
                    block.SetColor("_CustomColor3", color3);
                }, true);
            }
            else renderer.materials = getter.defaultMaterials;
        }
    }

    #region special

    /// <summary> Changes the style of the player. </summary>
    public void Style(Reader r)
    {
        customColors = r.Bool();
        color1 = r.Color(); color2 = r.Color(); color3 = r.Color();
        UpdateStyle();
    }

    /// <summary> Plays the punching animation and creates a shockwave as needed. </summary>
    public void Punch(Reader r)
    {
        var field = Tools.Field<Harpoon>("target");
        foreach (var harpoon in FindObjectsOfType<Harpoon>())
            if ((field.GetValue(harpoon) as EnemyIdentifierIdentifier)?.eid == EnemyId)
            {
                Bullets.Punch(harpoon);
                harpoon.name = "Net";
            }

        switch (r.Byte())
        {
            case 0:
                Animator.SetTrigger(r.Bool() ? "Parry" : "Punch");
                break;
            case 1:
                Instantiate(FistControl.Instance.redArm.ToAsset().GetComponent<Punch>().blastWave, r.Vector(), Quaternion.Euler(r.Vector())).name = "Net";
                break;
            case 2:
                var shock = Instantiate(NewMovement.Instance.gc.shockwave, transform.position, Quaternion.identity).GetComponent<PhysicalShockwave>();
                shock.name = "Net";
                shock.force = r.Float();
                break;
        }
    }

    /// <summary> Creates a pointer that will draw a line from itself to the player. </summary>
    public void Point(Reader r)
    {
        if (Pointer != null) Pointer.Lifetime = 4.5f;
        Pointer = Pointer.Spawn(Team, r.Vector(), r.Vector(), transform);
    }

    #endregion
    #region entity

    public override void Write(Writer w)
    {
        w.Float(x.Target); w.Float(y.Target); w.Float(z.Target);
        w.Float(bodyRotation.Target);
        w.Float(headRotation.Target);

        w.Byte(Health);
        w.Byte(RailCharge);
        w.Enum(Team);
        w.Byte(Weapon);
        w.Byte(Emoji);
        w.Byte(Rps);

        w.Bools(walking, sliding, falling, dashing, riding, inAir, shopping, Typing);

        w.Bool(usingHook);
        w.Float(hookX.Target); w.Float(hookY.Target); w.Float(hookZ.Target);
        w.Id(Pulled.Id);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r); y.Read(r); z.Read(r);
        bodyRotation.Read(r);
        headRotation.Read(r);

        Health = r.Byte();
        RailCharge = r.Byte();
        Team = r.Enum<Team>();
        Weapon = r.Byte();
        Emoji = r.Byte();
        Rps = r.Byte();

        r.Bools(out walking, out sliding, out falling, out dashing, out riding, out inAir, out shopping, out Typing);

        usingHook = r.Bool();
        hookX.Read(r); hookY.Read(r); hookZ.Read(r);

        var id = r.Id();
        if (id == 0L) Pulled.Value?.EnemyId?.gce.StopForceOff(); // player released the hook

        Pulled.Id = id;
    }

    public override void Damage(Reader r) => Bullets.DealDamage(EnemyId, r);

    public override void Kill()
    {
        Machine.GoLimp();
        Header.Hide();

        DestroyImmediate(this); // destroy the entity so that the indicators no longer point to it
        Events.OnTeamChanged.Fire();
    }

    #endregion
}
