namespace Jaket.Net.EntityTypes;

using Steamworks;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary>
/// Remote player that exists both on the local machine and on the remote one.
/// Responsible for the visual part of the player, i.e. model and animation, and for logic, i.e. health and teams.
/// </summary>
public class RemotePlayer : Entity
{
    /// <summary> Bundle containing assets for player doll. </summary>
    public static AssetBundle Bundle;

    /// <summary> Shader used by the game for materials. </summary>
    public static Shader Shader;

    /// <summary> Wing textures used to differentiate teams. </summary>
    public static Texture[] WingTextures;

    /// <summary> Player health, position and rotation. </summary>
    private FloatLerp health, x, y, z, bodyRotation, headRotation;

    /// <summary> Transforms of the head and the hand holding a weapon. </summary>
    private Transform head, hand;

    /// <summary> Last and current player team, needed for PvP mechanics. </summary>
    public Team lastTeam = (Team)0xFF, team;

    /// <summary> Last and current weapon id, needed only for visual. </summary>
    private byte lastWeapon = 0xFF, weapon;

    /// <summary> Material of the wings. </summary>
    private Material wingMaterial;

    /// <summary> Trail of the wings. </summary>
    private TrailRenderer wingTrail;

    /// <summary> Whether the player use custom weapon colors. </summary>
    private bool customColors;

    /// <summary> Custom weapon colors. </summary>
    private Color32 color1, color2, color3;

    /// <summary> Doll animator. Created by me in Unity and uploaded in mod via bundle. </summary>
    private Animator animator;

    /// <summary> Animator states that affect which animation will be played. </summary>
    private bool walking, sliding, wasInAir, inAir;

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

        // transforms
        head = transform.GetChild(0).GetChild(0).GetChild(2).GetChild(10).GetChild(0);
        hand = transform.GetChild(0).GetChild(0).GetChild(2).GetChild(5).GetChild(0).GetChild(0).GetChild(0);

        // other stuff
        wingMaterial = GetComponentInChildren<SkinnedMeshRenderer>().materials[1];
        wingTrail = GetComponentInChildren<TrailRenderer>();
        animator = GetComponentInChildren<Animator>();
        enemyId = GetComponent<EnemyIdentifier>();
        machine = GetComponent<Machine>();
        enemyId.weakPoint = head.gameObject;
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
        transform.eulerAngles = new(0f, bodyRotation.Get(LastUpdate), 0f);
        head.localEulerAngles = new(headRotation.Get(LastUpdate), 0f, 0f);

        if (lastTeam != team)
        {
            lastTeam = team;

            wingMaterial.mainTexture = WingTextures[team.Data().TextureId];
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

        if (wasInAir != inAir)
        {
            wasInAir = inAir;

            // fire the trigger if the player jumped
            if (inAir) animator.SetTrigger("Jump");
        }

        animator.SetBool("Walking", walking);
        animator.SetBool("Sliding", sliding);
        animator.SetBool("InAir", inAir);

        enemyId.health = machine.health = health.Get(LastUpdate);
        enemyId.dead = machine.health <= 0f;
        healthImage.localScale = new(machine.health / 100f, 1f, 1f);

        nicknameText.color = machine.health > 0f ? Color.white : Color.red;
        canvas.transform.LookAt(Camera.current.transform);
        canvas.transform.Rotate(new Vector3(0f, 180f, 0f), Space.Self);
    }

    public override void Write(Writer w)
    {
        w.Float(health.target);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);
        w.Float(head.localEulerAngles.x);

        w.Byte((byte)team);
        w.Byte(weapon);

        w.Bool(walking);
        w.Bool(sliding);
        w.Bool(inAir);
        w.Bool(typing);

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

        walking = r.Bool();
        sliding = r.Bool();
        inAir = r.Bool();
        typing = r.Bool();

        customColors = r.Bool();
        color1 = r.Color(); color2 = r.Color(); color3 = r.Color();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);

    #region instantiation

    /// <summary> Loads player doll prefab from the bundle. </summary>
    public static GameObject Prefab()
    {
        // cache the shader and the wing textures
        if (Shader == null || WingTextures == null)
        {
            var V2 = AssetHelper.LoadPrefab("cb3828ada2cbefe479fed3b51739edf6").GetComponent<V2>();

            Shader = V2.smr.material.shader;
            WingTextures = V2.wingTextures;
        }

        // if the bundle is already loaded, then there is no point in doing it again
        if (Bundle != null) return Bundle.LoadAsset<GameObject>("Player Doll.prefab");

        // location of Jaket.dll
        string assembly = Assembly.GetExecutingAssembly().Location;
        // mod folder
        string directory = assembly.Substring(0, assembly.LastIndexOf(Path.DirectorySeparatorChar));
        // location of bundle
        string bundle = Path.Combine(directory, "jaket-player-doll.bundle");

        Bundle = AssetBundle.LoadFromFile(bundle);
        return Bundle.LoadAsset<GameObject>("Player Doll.prefab");
    }

    /// <summary> Creates a new player doll from the prefab loaded from the bundle. </summary>
    public static RemotePlayer Create()
    {
        // create a doll from the prefab obtained from the bundle
        var obj = Object.Instantiate(Prefab(), Vector3.zero, Quaternion.identity);

        // it is necessary that the client doesn't consider the enemyId as a local object
        obj.name = "Net";

        // change the color of the material and its shader to match the style of the game
        foreach (var mat in obj.GetComponentInChildren<SkinnedMeshRenderer>().materials)
        {
            mat.color = Color.white;
            mat.shader = Shader;
        }

        // add components
        var enemyId = obj.AddComponent<EnemyIdentifier>();
        var machine = obj.AddComponent<Machine>();

        enemyId.enemyClass = EnemyClass.Machine;
        enemyId.enemyType = EnemyType.V2;
        enemyId.weaknesses = new string[0];
        enemyId.burners = new();
        machine.destroyOnDeath = new GameObject[0];
        machine.hurtSounds = new AudioClip[0];

        // add enemy identifier to all doll parts so that bullets can hit it
        foreach (var rigidbody in obj.transform.GetChild(0).GetChild(0).GetComponentsInChildren<Rigidbody>())
        {
            rigidbody.gameObject.AddComponent<EnemyIdentifierIdentifier>();
            rigidbody.gameObject.tag = MapTag(rigidbody.gameObject.tag);
        }

        // add a script to further control the doll
        return obj.AddComponent<RemotePlayer>();
    }

    /// <summary> Tags after loading from a bundle changes due to a mismatch in the tags list, this method returns everything to its place. </summary>
    public static string MapTag(string tag) => tag switch
    {
        "RoomManager" => "Body",
        "Body" => "Limb",
        "Forward" => "Head",
        _ => tag
    };

    #endregion
}
