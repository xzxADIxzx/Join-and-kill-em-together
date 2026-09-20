namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.UI.Elements;

/// <summary>
/// Tangible entity of the player type.
/// Responsible for the logical part of the player.
/// </summary>
public class RemotePlayer : Entity
{
    Agent agent;
    Float bodyX, bodyY, bodyZ, hookX, hookY, hookZ, bodyRotation, headRotation;
    Vector3 gravity;
    EnemyIdentifier enemyId;
    Collider[] cs;

    /// <summary> Health of the player, usually varies between zero and two hundred. </summary>
    public byte Health;
    /// <summary> Charge of the railgun, always varies between zero and ten. </summary>
    public byte Charge;

    /// <summary> Channels playing various sound effects. </summary>
    public AudioSource[] Audio;
    /// <summary> Whether the player is typing a message. </summary>
    public bool Typing;

    /// <summary> Doll that displays the state of the player via animations. </summary>
    public Doll Doll = new();
    /// <summary> Label that displays the nickname and health of the player. </summary>
    public Header Header = new();
    /// <summary> Last point created by the player. </summary>
    public Point Point;
    /// <summary> Last spray created by the player. </summary>
    public Spray Spray;

    /// <inheritdoc cref="Doll.Team"/>
    public Team Team => Doll.Team;
    /// <inheritdoc cref="Header.Name"/>
    public string Name => Header.Name;

    public RemotePlayer(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 40;

    public override void Write(Writer w)
    {
        w.Floats(bodyX, bodyY, bodyZ);
        w.Floats(hookX, hookY, hookZ);

        w.Floats(bodyRotation);
        w.Floats(headRotation);

        w.Byte(Health);
        w.Byte(Charge);

        w.State(Doll.Emote, Doll.Rps, Typing, gravity);
        w.Bools(Doll.Walking, Doll.Sliding, Doll.Falling, Doll.Slaming, Doll.Riding, Doll.Hooking, Doll.Shopping);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        if (bodyX.Jump || bodyY.Jump || bodyZ.Jump) Events.Post(Doll.Clear);

        r.Floats(ref bodyX, ref bodyY, ref bodyZ);
        r.Floats(ref hookX, ref hookY, ref hookZ);

        r.Floats(ref bodyRotation);
        r.Floats(ref headRotation);

        Health = r.Byte();
        Charge = r.Byte();

        r.State(out Doll.Emote, out Doll.Rps, out Typing, out gravity);
        r.Bools(out Doll.Walking, out Doll.Sliding, out Doll.Falling, out Doll.Slaming, out Doll.Riding, out Doll.Hooking, out Doll.Shopping, out _);
    }

    #endregion
    #region properties

    public override bool Debuggable => agent;

    public override Vector3 DrawPos => agent.Position;

    public virtual Vector3 DrawCntr => agent.Position + agent.transform.up * 2.5f;

    public virtual EnemyTarget Target => new(enemyId);

    #endregion
    #region logic

    public override void Create() => Create(Entities.Players, ref bodyX, ref bodyY, ref bodyZ);

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out enemyId);
        agent.Get(out cs);
        agent.Get(out Audio);

        Doll.Assign(agent);
        Header.Assign(this);
    }

    public override void Update(float delta)
    {
        if (enemyId == null || enemyId.machine.limp)
        {
            if (Health != 0)
            {
                agent?.Rem(true);
                Create();
            }
            return;
        }
        else if (Health == 0) Doll.Killed(default, default);

        agent.Position     = new(bodyX.GetAware(delta), bodyY.GetAware(delta), bodyZ.GetAware(delta));
        Doll.Hook.position = new(hookX.GetAware(delta), hookY.GetAware(delta), hookZ.GetAware(delta));

        agent.Rotation = ( Quaternion.Euler(gravity) * Quaternion.AngleAxis(bodyRotation.GetAngle(delta), Vector3.up) ).eulerAngles;
        Doll.HeadAngle = headRotation.GetAngle(delta);

        Doll.Update(delta);

        enemyId.machine.health = 4242f; // hacky
        if (Doll.Falling) enemyId.timeSinceSpawned = 0f;
    }

    public override void Damage(Reader r) => Entities.Damage.Deal(enemyId, r.Float());

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Header.Hide();
        Doll.Killed(default, default);
        Dest(agent);
        Events.OnTeamChange.Fire();
    }

    #endregion
    #region other

    public void Toggle(bool on) => cs.Each(c => c, c => c.enabled = on);

    public void Toggle(Collider other) => cs.Each(c => c, c => Physics.IgnoreCollision(c, other, Team.Ally()));

    #endregion
}
