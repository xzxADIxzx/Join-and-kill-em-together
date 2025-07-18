namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI.Elements;

/// <summary>
/// There are multiple instances of this entity, representing players whose machines are connected to the local one.
/// The snapshot structure of this entity is identical to the local player structure.
/// </summary>
public class RemotePlayer : Entity
{
    Agent agent;
    Float bodyX, bodyY, bodyZ, hookX, hookY, hookZ, bodyRotation, headRotation;
    EnemyIdentifier enemyId;

    /// <summary> Health of the player, usually varies between zero and two hundred. </summary>
    public byte Health = 100;
    /// <summary> Charge of the railgun, always varies between zero and ten. </summary>
    public byte Charge;

    /// <summary> Team required for versus mechanics. </summary>
    public Team Team, LastTeam;
    /// <summary> Identifier of the displayed weapon. </summary>
    public byte Weapon, LastWeapon;

    /// <summary> Source playing the voice of the player. </summary>
    public AudioSource Voice;
    /// <summary> Whether the player is typing a message. </summary>
    public bool Typing;

    /// <summary> Doll that displays the state of the player via animations. </summary>
    public Doll Doll;
    /// <summary> Label that displays the nickname and health of the player. </summary>
    public Header Header;
    /// <summary> Last point created by the player. </summary>
    public Pointer Point;
    /// <summary> Last spray created by the player. </summary>
    public Spray Spray;

    public RemotePlayer(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 42;

    public override void Write(Writer w)
    {
        w.Floats(bodyX, bodyY, bodyZ);
        w.Floats(hookX, hookY, hookZ);

        w.Float(bodyRotation.Next);
        w.Float(headRotation.Next);

        w.Byte(Health);
        w.Byte(Charge);

        w.Player(Team, Weapon, Doll.Emote, Doll.Rps, Typing);
        Doll.WriteAnim(w);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        r.Floats(ref bodyX, ref bodyY, ref bodyZ);
        r.Floats(ref hookX, ref hookY, ref hookZ);

        bodyRotation.Set(r.Float());
        headRotation.Set(r.Float());

        Health = r.Byte();
        Charge = r.Byte();

        if (Doll == null) return;

        r.Player(out Team, out Weapon, out Doll.Emote, out Doll.Rps, out Typing);
        Doll.ReadAnim(r);
    }

    #endregion
    #region logic

    public override void Create() => Assign(ModAssets.CreateDoll(new(bodyX.Prev = bodyX.Next, bodyY.Prev = bodyY.Next, bodyZ.Prev = bodyZ.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.TryGetComponent(out enemyId);
        agent.TryGetComponent(out Voice);

        Doll ??= new(() =>
        {
            // recreate the weapon if the animation is over
            if (Doll.Emote == 0xFF) LastWeapon = 0xFF;
            // or destroy it if the animation has started
            else Doll.Hand.Each(Dest);
        });
        Doll.Assign(agent.transform);

        Header ??= new(this);
        Header.Assign(agent.transform);

        enemyId.tag = "Enemy";
        enemyId.weakPoint = Doll.Head.gameObject;

        LastTeam = (Team)0xFF;
        LastWeapon = 0xFF;
    }

    public override void Update(float delta)
    {
        if (Doll.Animator == null)
        {
            if (Health != 0) // the player has respawned, the agent needs to be recreated
            {
                Dest(agent.gameObject);
                Create();
            }
            return;
        }
        else if (Health == 0) Disassemble();

        agent.Position     = new(bodyX.Get(delta), bodyY.Get(delta), bodyZ.Get(delta));
        Doll.Hook.position = new(hookX.Get(delta), hookY.Get(delta), hookZ.Get(delta));
        agent.Rotation = new(0f, bodyRotation.GetAngle(delta));
        Doll.HeadAngle =         headRotation.GetAngle(delta);

        Doll.Hook.LookAt(agent.Position);
        Doll.Hook.Rotate(Vector3.up * 180f, Space.Self);
        Doll.HookWinch.SetPosition(0, Doll.HookRoot.position);
        Doll.HookWinch.SetPosition(1, Doll.Hook.position);

        Doll.Update();
        enemyId.machine.health = 4200f;

        if (LastTeam != Team)
        {
            Doll.ApplyTeam(LastTeam = Team);
            Events.OnTeamChange.Fire();
        }
        if (LastWeapon != Weapon)
        {
            Doll.ApplyItem(LastWeapon = Weapon);
            Doll.ApplySuit();
        }
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r); // TODO Damage class

    public override void Killed(Reader r, int left)
    {
        // the player is already destroyed
        if (agent == null) return;

        Hidden = true;
        Header.Hide();
        Disassemble();
        Dest(agent);
        Dest(Doll.Hand.gameObject);
        Events.OnTeamChange.Fire();
    }

    #endregion
    #region other

    /// <summary> Approximate position of the player used by spectators and indicators. </summary>
    public Vector3 Position => agent == null ? Vector3.zero : agent.Position + Vector3.up * 2.5f;

    /// <summary> Breaks the player doll into multiple peaces. </summary>
    public void Disassemble()
    {
        // destroy the animation controller and rigdol the model
        enemyId.machine.GoLimp();

        if (Doll.WingLight)    Dest(Doll.WingLight);
        if (Doll.SlidParticle) Dest(Doll.SlidParticle.gameObject);
        if (Doll.SlamParticle) Dest(Doll.SlamParticle.gameObject);
    }

    /// <summary> Plays the punching animation and creates a shockwave as needed. </summary>
    public void Punch(Reader r)
    {
        var field = Field<Harpoon>("target");
        foreach (var harpoon in Object.FindObjectsOfType<Harpoon>())
            if ((field.GetValue(harpoon) as EnemyIdentifierIdentifier)?.eid == enemyId) Bullets.Punch(harpoon, false);

        switch (r.Byte())
        {
            case 0:
                Doll.Animator.SetTrigger(r.Bool() ? "parry" : "punch");
                break;
            case 1:
                var blast = Inst(GameAssets.Blast(), r.Vector(), Quaternion.Euler(r.Vector()));
                blast.name = "Net";
                blast.GetComponentInChildren<Explosion>().sourceWeapon = Bullets.Fake;
                break;
            case 2:
                var shock = Inst(NewMovement.Instance.gc.shockwave, agent.Position).GetComponent<PhysicalShockwave>();
                shock.name = "Net";
                shock.force = r.Float();
                break;
        }
    }

    #endregion
}
