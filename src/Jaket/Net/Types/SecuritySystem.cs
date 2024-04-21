namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.IO;

/// <summary> Representation of one of the seven parts of EarthMover security systems. </summary>
public class SecuritySystem : Entity
{
    /// <summary> This is the only value that should be synchronized. </summary>
    private FloatLerp health = new();

    private void Awake()
    {
        Init(_ => Type);

        LobbyController.ScaleHealth(ref EnemyId.machine.health);
        health.target = EnemyId.machine.health;
    }

    private void Update()
    {
        if (!LobbyController.IsOwner) EnemyId.machine.health = health.Get(LastUpdate);
    }

    #region entity

    public override void Write(Writer w) => w.Float(EnemyId.machine.health);

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;
        health.Read(r);
    }

    public override void Kill() => EnemyId.InstaKill();

    #endregion
}
