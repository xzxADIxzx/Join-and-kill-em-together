namespace Jaket.Net.EntityTypes;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary>
/// Remote player that exists both on the local machine and on the remote one.
/// Responsible for the visual part of the player, i.e. model and animation, and for logic, i.e. health and teams.
/// </summary>
public class RemotePlayer : Entity
{
    /// <summary> Player health, position and rotation. </summary>
    private FloatLerp health, x, y, z, bodyRotation, headRotation, hookX, hookY, hookZ;

    /// <summary> Transforms of the head and the hand holding a weapon. </summary>
    private Transform head, hand, hook, hookRoot;

    /// <summary> Last and current player team, needed for PvP mechanics. </summary>
    public Team lastTeam = (Team)0xFF, team;

    /// <summary> Last and current weapon id, needed only for visual. </summary>
    private byte lastWeapon = 0xFF, weapon;

    /// <summary> Last and current emoji id, needed only for fun. </summary>
    private byte lastEmoji = 0xFF, emoji;

    /// <summary> Material of the wings. </summary>
    private Material wingMaterial;

    /// <summary> Trail of the wings. </summary>
    private TrailRenderer wingTrail;

    /// <summary> Winch of the hook. </summary>
    private LineRenderer hookWinch;

    /// <summary> Whether the player use custom weapon colors. </summary>
    private bool customColors;

    /// <summary> Custom weapon colors. </summary>
    private Color32 color1, color2, color3;

    /// <summary> Doll animator. Created by me in Unity and uploaded in mod via bundle. </summary>
    private Animator animator;

    /// <summary> Animator states that affect which animation will be played. </summary>
    private bool walking, sliding, falling, wasInAir, inAir, wasUsingHook, usingHook;

    /// <summary> Slide and fall particle transforms. </summary>
    private Transform slideParticle, fallParticle;

    /// <summary> Enemy component of the player doll. </summary>
    private EnemyIdentifier enemyId;

    /// <summary> Machine component of the player doll. </summary>
    private Machine machine;

    /// <summary> Player name. Taken from Steam. </summary>
    public string nickname;

    /// <summary> Whether the player is typing a message. </summary>
    public bool typing;

    /// <summary> Canvas containing nickname. </summary>
    private GameObject canvas;

    /// <summary> Text containing nickname. </summary>
    private Text nicknameText;

    /// <summary> Image showing health. </summary>
    private RectTransform healthImage;

    private void Awake()
    {
        // interpolations
        health = new();
        x = new();
        y = new();
        z = new();
        bodyRotation = new();
        headRotation = new();
        hookX = new();
        hookY = new();
        hookZ = new();

        // transforms
        head = transform.GetChild(0).GetChild(0).GetChild(4).GetChild(10).GetChild(0);
        hand = transform.GetChild(0).GetChild(0).GetChild(4).GetChild(5).GetChild(0).GetChild(0);
        hand = Utils.Object("Weapons", hand).transform;
        hook = transform.GetChild(0).GetChild(0).GetChild(1);
        hookRoot = transform.GetChild(0).GetChild(0).GetChild(4).GetChild(0).GetChild(0).GetChild(0).GetChild(0);

        // other stuff
        wingMaterial = GetComponentInChildren<SkinnedMeshRenderer>().materials[1];
        wingTrail = GetComponentInChildren<TrailRenderer>();
        hookWinch = GetComponentInChildren<LineRenderer>(true);
        animator = GetComponentInChildren<Animator>();
        enemyId = GetComponent<EnemyIdentifier>();
        machine = GetComponent<Machine>();

        enemyId.weakPoint = head.gameObject;
        hookWinch.material = HookArm.Instance.GetComponent<LineRenderer>().material;
    }

    private void Start()
    {
        // nickname
        nickname = new Friend(Id).Name;
        float width = nickname.Length * 14f + 16f;

        canvas = Utils.Canvas("Nickname", transform, width, 64f, new Vector3(0f, 5f, 0f));
        nicknameText = Utils.Button(nickname, canvas.transform, 0f, 0f, width, 40f, 24, Color.white, TextAnchor.MiddleCenter, () => {}).GetComponentInChildren<Text>();

        Utils.Image("Health Background", canvas.transform, 0f, -30f, width - 16f, 4f);
        healthImage = Utils.Image("Health", canvas.transform, 0f, -30f, width - 16f, 4f, Color.red).transform as RectTransform;

        // for some unknown reason, the canvas needs to be scaled after adding elements
        canvas.transform.localScale = new Vector3(.02f, .02f, .02f);

        // idols can target players, which is undesirable
        int index = EnemyTracker.Instance.enemies.IndexOf(enemyId);
        if (index != -1)
        {
            EnemyTracker.Instance.enemies.RemoveAt(index);
            EnemyTracker.Instance.enemyRanks.RemoveAt(index);
        }
    }

    private void Update()
    {
        // if animator is null, then the player is dead, but if health is greater than zero, then he has revived
        if (health.target > 0f && animator == null)
        {
            Destroy(gameObject); // destroy the doll so that the client restore it
            return;
        }

        // prevent null pointer
        if (animator == null) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate) - (sliding ? .3f : 1.5f), z.Get(LastUpdate));
        transform.eulerAngles = new(0f, bodyRotation.GetAngel(LastUpdate), 0f);
        head.localEulerAngles = new(headRotation.Get(LastUpdate), 0f, 0f);

        if (lastTeam != team)
        {
            lastTeam = team;

            wingMaterial.mainTexture = DollAssets.WingTextures[(int)team];
            wingMaterial.color = team.Data().WingColor(); // do this after changing the wings texture

            var color = team.Data().Color();
            wingTrail.startColor = new Color(color.r, color.g, color.b, .5f);

            // update player indicators to only show teammates
            PlayerIndicators.Instance.Rebuild();
        }

        gameObject.tag = team == Networking.LocalPlayer.team ? "Untagged" : "Enemy"; // toggle friendly fire

        if (lastWeapon != weapon)
        {
            lastWeapon = weapon;

            foreach (Transform child in hand) Destroy(child.gameObject);
            if (weapon != 0xFF)
            {
                Weapons.Instantiate(weapon, hand);
                WeaponsOffsets.Apply(weapon, hand);

                // sync weapon colors
                foreach (var getter in hand.GetComponentsInChildren<GunColorGetter>())
                {
                    var renderer = getter.GetComponent<Renderer>();

                    if (customColors)
                    {
                        renderer.materials = getter.coloredMaterials;
                        foreach (var mat in renderer.materials)
                        {
                            mat.SetColor("_CustomColor1", color1);
                            mat.SetColor("_CustomColor2", color2);
                            mat.SetColor("_CustomColor3", color3);
                        }
                    }
                    else renderer.materials = getter.defaultMaterials;
                }
            }
        }

        if (lastEmoji != emoji)
        {
            lastEmoji = emoji;

            animator.SetTrigger("Show Emoji");
            animator.SetInteger("Emoji", emoji);

            // recreate the weapon if the animation is over
            if (emoji == 0xFF) lastWeapon = 0xFF;
            // or destroy it if the animation has started
            else foreach (Transform child in hand) Destroy(child.gameObject);
        }

        if (wasInAir != inAir)
        {
            wasInAir = inAir;

            // fire the trigger if the player jumped
            if (inAir) animator.SetTrigger("Jump");
        }

        if (wasUsingHook != usingHook)
        {
            wasUsingHook = usingHook;

            // fire the trigger if the player threw a hook
            if (usingHook) animator.SetTrigger("Throw Hook");

            // toggle the visibility of the hook
            hook.gameObject.SetActive(usingHook);
        }

        animator.SetBool("Walking", walking);
        animator.SetBool("Sliding", sliding);
        animator.SetBool("InAir", inAir);
        animator.SetBool("UsingHook", usingHook);

        if (sliding && slideParticle == null)
        {
            slideParticle = Instantiate(NewMovement.Instance.slideParticle, transform).transform;
            slideParticle.localPosition = new(0f, 0f, 2f);
            slideParticle.localEulerAngles = new(0f, 180f, 0f);
            slideParticle.localScale = new(1f, 1f, .5f);
        }
        else if (!sliding && slideParticle != null)
            Destroy(slideParticle.gameObject);

        if (falling && fallParticle == null)
        {
            fallParticle = Instantiate(NewMovement.Instance.fallParticle, transform).transform;
            fallParticle.localPosition = new(0f, 5f, 0f);
            fallParticle.localEulerAngles = new(90f, 0f, 0f);
            fallParticle.localScale = new(1f, .5f, 1f);
        }
        else if (!falling && fallParticle != null)
            Destroy(fallParticle.gameObject);

        enemyId.health = machine.health = health.Get(LastUpdate);
        enemyId.dead = machine.health <= 0f;
        healthImage.localScale = new(machine.health / 100f, 1f, 1f);

        nicknameText.color = machine.health > 0f ? Color.white : Color.red;
        canvas.transform.LookAt(Camera.current.transform);
        canvas.transform.Rotate(new(0f, 180f, 0f), Space.Self);

        // sometimes the player does not crumble after death
        if (enemyId.health <= 0f && !machine.limp) machine.GoLimp();
    }

    private void LateUpdate()
    {
        // everything related to the hook is in LateUpdate, because it is a child of the player's doll and moves with it
        hook.position = new(hookX.Get(LastUpdate), hookY.Get(LastUpdate), hookZ.Get(LastUpdate));
        hook.LookAt(transform);
        hook.Rotate(new(0f, 180f, 0f), Space.Self);

        hookWinch.SetPosition(0, hookRoot.position);
        hookWinch.SetPosition(1, hook.position);
    }

    public override void Write(Writer w)
    {
        w.Float(health.target);
        w.Float(x.target); w.Float(y.target); w.Float(z.target);
        w.Float(bodyRotation.target);
        w.Float(headRotation.target);

        w.Byte((byte)team);
        w.Byte(weapon);
        w.Byte(emoji);

        w.Bool(walking);
        w.Bool(sliding);
        w.Bool(falling);
        w.Bool(inAir);
        w.Bool(typing);

        w.Bool(usingHook);
        w.Float(hookX.target); w.Float(hookY.target); w.Float(hookZ.target);

        w.Bool(customColors);
        w.Color(color1); w.Color(color2); w.Color(color3);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        bodyRotation.Read(r);
        headRotation.Read(r);

        team = (Team)r.Byte();
        weapon = r.Byte();
        emoji = r.Byte();

        walking = r.Bool();
        sliding = r.Bool();
        falling = r.Bool();
        inAir = r.Bool();
        typing = r.Bool();

        usingHook = r.Bool();
        hookX.Read(r); hookY.Read(r); hookZ.Read(r);

        customColors = r.Bool();
        color1 = r.Color(); color2 = r.Color(); color3 = r.Color();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);

    public void Punch(Reader r)
    {
        switch (r.Byte())
        {
            case 0:
                animator.SetTrigger("Punch");
                break;
            case 1:
                Instantiate(FistControl.Instance.redArm.GetComponent<Punch>().blastWave, r.Vector(), Quaternion.Euler(r.Vector())).name = "Net";
                break;
            case 2:
                var shock = Instantiate(NewMovement.Instance.gc.shockwave, transform.position, Quaternion.identity);
                shock.name = "Net";
                shock.GetComponent<PhysicalShockwave>().force = r.Float();
                break;
        }
    }
}
