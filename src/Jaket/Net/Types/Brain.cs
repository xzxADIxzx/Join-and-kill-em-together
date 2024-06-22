namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Brain representation responsible for synchronizing health and idols. </summary>
public class Brain : Entity
{
    /// <summary> Earthmover's brain health. </summary>
    private FloatLerp health = new();
    /// <summary> Idols' state. </summary>
    private bool idol1, idol2;
    /// <summary> Idols' doors. </summary>
    private Door door1, door2;

    private void Awake()
    {
        Init(_ => EntityType.Brain);

        transform.parent.Find("IdolPod").GetChild(0).TryGetComponent(out door1);
        transform.parent.Find("IdolPod (1)").GetChild(0).TryGetComponent(out door2);

        if (LobbyController.IsOwner)
            LobbyController.ScaleHealth(ref EnemyId.machine.health);

        World.Brain = this;
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        EnemyId.machine.health = health.Get(LastUpdate);

        if (door1.open && !idol1) door1.Close();
        if (door2.open && !idol2) door2.Close();
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Float(EnemyId.health);
        w.Bool(door1.open);
        w.Bool(door2.open);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        idol1 = r.Bool();
        idol2 = r.Bool();
    }

    public override void Kill(Reader r) => EnemyId.InstaKill();

    #endregion
}
