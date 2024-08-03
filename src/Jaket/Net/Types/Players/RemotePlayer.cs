namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI.Elements;

/// <summary>
/// Remote player that exists both on the local machine and on the remote one.
/// Responsible for the visual part of the player, i.e. model and animation, and for logic, i.e. health and teams.
/// </summary>
public class RemotePlayer : Entity
{
    /// <summary> Position of the player, the rotation of its body and head, and the position of the hook. </summary>
    private FloatLerp x, y, z, bodyRotation, headRotation, hookX, hookY, hookZ;

    /// <summary> Health may not match the real one due to byte limitations. </summary>
    public byte Health = 100;
    /// <summary> Player's railgun charge. From 0 to 10. </summary>
    public byte RailCharge;

    /// <summary> Player team needed for PvP mechanics. </summary>
    public Team Team, LastTeam = (Team)0xFF;
    /// <summary> Weapon id needed only for visual. </summary>
    public byte Weapon, LastWeapon = 0xFF;

    /// <summary> Component responsible for playing Sam's voice. </summary>
    public AudioSource Voice;
    /// <summary> Whether the player is typing a message. </summary>
    public bool Typing;

    /// <summary> Doll of the player, displaying its state through animations. </summary>
    public Doll Doll;
    /// <summary> Header displaying nickname and health. </summary>
    public PlayerHeader Header;
    /// <summary> Last pointer created by the player. </summary>
    public Pointer Pointer;

    private void Awake()
    {
        Init(null, true);
        TryGetComponent(out Voice);

        x = new(); y = new(); z = new();
        bodyRotation = new();
        headRotation = new();
        hookX = new(); hookY = new(); hookZ = new();
    }

    private void Start()
    {
        Doll = gameObject.AddComponent<Doll>();
        Doll.OnEmojiStart += () =>
        {
            // recreate the weapon if the animation is over
            if (Doll.Emoji == 0xFF) LastWeapon = 0xFF;
            // or destroy it if the animation has started
            else foreach (Transform child in Doll.Hand) Destroy(child.gameObject);
        };
        Header = new(Owner = Id, transform);
        tag = "Enemy";

        EnemyId.weakPoint = Doll.Head.gameObject;
        Doll.HookWinch.material = HookArm.Instance.GetComponent<LineRenderer>().material;
        ClearTrail(Doll.WingTrail, x, y, z);

        // idols can target players, which is undesirable
        int index = EnemyTracker.Instance.enemies.IndexOf(EnemyId);
        if (index != -1)
        {
            EnemyTracker.Instance.enemies.RemoveAt(index);
            EnemyTracker.Instance.enemyRanks.RemoveAt(index);
        }
    }

    private void Update() => Stats.MTE(() =>
    {
        Header.Update(Health, Typing);
        if (Animator == null) // the player is dead
        {
            if (Health != 0) Destroy(gameObject); // the player has respawned, the doll needs to be recreated
            return;
        }
        else if (Health == 0) EnemyId.machine.GoLimp();

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate) - (Doll.Sliding ? .3f : 1.5f), z.Get(LastUpdate));
        transform.eulerAngles = new(0f, bodyRotation.GetAngel(LastUpdate));
        Doll.Head.localEulerAngles = new(Doll.Emoji == 8 ? -20f : headRotation.Get(LastUpdate), 0f);

        EnemyId.machine.health = 4200f; // prevent the doll from dying too early

        if (LastTeam != Team)
        {
            Doll.ApplyTeam(LastTeam = Team);
            Events.OnTeamChanged.Fire();
        }
        if (LastWeapon != Weapon)
        {
            foreach (Transform child in Doll.Hand) Destroy(child.gameObject);
            if ((LastWeapon = Weapon) != 0xFF)
            {
                Weapons.Instantiate(Weapon, Doll.Hand);
                WeaponsOffsets.Apply(Weapon, Doll.Hand);
                Doll.ApplySuit();
            }
        }
    });

    private void LateUpdate() => Stats.MTE(() =>
    {
        // everything related to the hook is in LateUpdate, because it is a child of the player's doll and moves with it
        Doll.Hook.position = new(hookX.Get(LastUpdate), hookY.Get(LastUpdate), hookZ.Get(LastUpdate));
        Doll.Hook.LookAt(transform);
        Doll.Hook.Rotate(Vector3.up * 180f, Space.Self);

        Doll.HookWinch.SetPosition(0, Doll.HookRoot.position);
        Doll.HookWinch.SetPosition(1, Doll.Hook.position);
    });

    #region special

    /// <summary> Plays the punching animation and creates a shockwave as needed. </summary>
    public void Punch(Reader r)
    {
        var field = Tools.Field<Harpoon>("target");
        foreach (var harpoon in FindObjectsOfType<Harpoon>())
            if ((field.GetValue(harpoon) as EnemyIdentifierIdentifier)?.eid == EnemyId) Bullets.Punch(harpoon, false);

        switch (r.Byte())
        {
            case 0:
                Animator.SetTrigger(r.Bool() ? "parry" : "punch");
                break;
            case 1:
                var blast = Instantiate(GameAssets.Blast(), r.Vector(), Quaternion.Euler(r.Vector()));
                blast.name = "Net";
                blast.GetComponentInChildren<Explosion>().sourceWeapon = Bullets.Fake;
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
        UpdatesCount++;

        w.Float(x.Target); w.Float(y.Target); w.Float(z.Target);
        w.Float(bodyRotation.Target);
        w.Float(headRotation.Target);
        w.Float(hookX.Target); w.Float(hookY.Target); w.Float(hookZ.Target);

        w.Byte(Health);
        w.Byte(RailCharge);

        if (!Doll) return;

        w.Player(Team, Weapon, Doll.Emoji, Doll.Rps, Typing);
        Doll.WriteAnim(w);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r); y.Read(r); z.Read(r);
        bodyRotation.Read(r);
        headRotation.Read(r);
        hookX.Read(r); hookY.Read(r); hookZ.Read(r);

        Health = r.Byte();
        RailCharge = r.Byte();

        if (!Doll || r.Position >= r.Length) return;

        r.Player(out Team, out Weapon, out Doll.Emoji, out Doll.Rps, out Typing);
        Doll.ReadAnim(r);
    }

    public override void Kill(Reader r = null)
    {
        EnemyId.machine.GoLimp();
        Header.Hide();

        Destroy(Doll.Hand.gameObject); // destroy the weapon so that the railcannon's sound doesn't play forever
        DestroyImmediate(this); // destroy the entity so that the indicators no longer point to it
        Events.OnTeamChanged.Fire();
    }

    #endregion
}
