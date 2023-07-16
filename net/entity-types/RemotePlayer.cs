namespace Jaket.Net.EntityTypes;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

public class RemotePlayer : Entity
{
    // please don't ask me how I found this (4368)
    const string V2AssetKey = "cb3828ada2cbefe479fed3b51739edf6";

    /// <summary> Player name. Taken from Steam. </summary>
    public string nickname;

    /// <summary> Whether the player is typing a message. </summary>
    public bool typing;

    /// <summary> Player doll animations. </summary>
    private Animator anim;

    /// <summary> Player doll machine. </summary>
    private Machine machine;

    /// <summary> Player doll enemy identifier. </summary>
    private EnemyIdentifier enemyId;

    /// <summary> Transform to which weapons will be attached. </summary>
    private Transform weapons;

    /// <summary> Player health. </summary>
    private FloatLerp health;

    /// <summary> Player position and rotation. </summary>
    private FloatLerp x, y, z, rotation;

    /// <summary> Animator states. </summary>
    private bool walking, sliding;

    /// <summary> Last and current weapon id. </summary>
    private int lastWeapon, weapon;

    /// <summary> Canvas containing nickname. </summary>
    private GameObject canvas;

    /// <summary> Text containing nickname. </summary>
    private Text nicknameText;

    /// <summary> Image showing health. </summary>
    private RectTransform healthImage;

    /// <summary> Creates a new remote player doll. </summary>
    public static RemotePlayer CreatePlayer()
    {
        var prefab = AssetHelper.LoadPrefab(V2AssetKey);
        var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        obj.name = "Net";

        return obj.AddComponent<RemotePlayer>();
    }

    public void Damage(float damage) => Networking.Send(LobbyController.Owner, Writer.Write(w =>
    {
        w.Id(Owner); // target
        w.Float(damage); // damage
    }), PacketType.DamagePlayer);

    public void Awake()
    {
        Type = EntityType.Player;
        Networking.Players.Add(Owner, this);

        anim = GetComponentInChildren<Animator>();
        machine = GetComponent<Machine>();
        enemyId = GetComponent<EnemyIdentifier>();

        weapons = gameObject.GetComponent<V2>().weapons[0].transform.parent;
        foreach (Transform child in weapons) Destroy(child.gameObject);
        weapons = Utils.Object("Transform", weapons).transform;

        health = new FloatLerp();
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();

        Destroy(gameObject.GetComponent<V2>()); // remove ai
        Destroy(gameObject.GetComponentInChildren<V2AnimationController>());

        // nickname
        nickname = new Friend(Owner).Name;
        float width = nickname.Length * 14f + 16f;

        canvas = Utils.Canvas("Nickname", transform, width, 64f, new Vector3(0f, 5f, 0f));
        nicknameText = Utils.Button(nickname, canvas.transform, 0f, 0f, width, 40f, 24, Color.white, TextAnchor.MiddleCenter, () => {}).GetComponentInChildren<Text>();

        Utils.Image("Health Background", canvas.transform, 0f, -30f, width - 16f, 4f, new Color(0f, 0f, 0f, .5f));
        healthImage = Utils.Image("Health", canvas.transform, 0f, -30f, width - 16f, 4f, Color.red).GetComponent<RectTransform>();

        // for some unknown reason, the canvas needs to be scaled after adding elements
        canvas.transform.localScale = new Vector3(.02f, .02f, .02f);
    }

    public void Update()
    {
        // health & position
        machine.health = health.Get(LastUpdate);
        enemyId.dead = machine.health <= 0f;
        healthImage.localScale = new Vector3(machine.health / 100f, 1f, 1f);

        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate) - (sliding ? 0f : 1.6f), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);

        // animation
        anim.SetBool("RunningBack", sliding);
        anim.SetBool("Sliding", sliding);

        if (lastWeapon != weapon && weapon != -1)
        {
            lastWeapon = weapon;

            foreach (Transform child in weapons) Destroy(child.gameObject);
            if (weapon != -1)
            {
                Weapons.Instantiate(weapon, weapons);
                WeaponsOffsets.Apply(weapon, weapons);
            }
        }

        // nickname
        nicknameText.color = machine.health > 0 ? Color.white : Color.red;
        canvas.transform.LookAt(Camera.current.transform);
        canvas.transform.Rotate(new Vector3(0f, 180f, 0f), Space.Self);
    }

    public override void Write(Writer w)
    {
        // health & position
        w.Float(machine.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);

        // animation
        w.Bool(typing);
        w.Bool(walking);
        w.Bool(sliding);
        w.Int(weapon);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        // health & position 
        health.Read(r);
        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);

        // animation
        typing = r.Bool();
        walking = r.Bool();
        sliding = r.Bool();

        lastWeapon = weapon;
        weapon = r.Int();
    }
}