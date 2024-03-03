namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Minos hand representation responsible for synchronizing animations and attacks. </summary>
public class Minotaur : Entity
{
    /// <summary> Boss controller containing methods for attacks. </summary>
    private MinotaurChase minotaur;

    /// <summary> Minotaur health, position and rotation. </summary>
    private FloatLerp health, x, y, z, rotation;
    /// <summary> Id of the attack. </summary>
    public byte Attack = 0xFF, LastAttack = 0xFF;

    private void Awake()
    {
        Init(_ => EntityType.Minotaur_Chase);

        health = new();
        x = new(); y = new(); z = new();
        rotation = new();

        minotaur = GetComponent<MinotaurChase>();

        if (LobbyController.IsOwner)
            LobbyController.ScaleHealth(ref EnemyId.machine.health);

        World.Instance.Minotaur = this;
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        EnemyId.machine.health = health.Get(LastUpdate);
        EnemyId.dead = EnemyId.machine.health <= 0f;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(0f, rotation.GetAngel(LastUpdate), 0f);

        if (LastAttack != Attack) switch (LastAttack = Attack)
            {
                case 0: Tools.Invoke("HammerSwing", minotaur); break;
                case 1: Tools.Invoke("MeatThrow", minotaur); break;
                case 2: Tools.Invoke("HandSwing", minotaur); break;
            }
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Float(EnemyId.health);
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);
        w.Byte(Attack);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        rotation.Read(r);
        Attack = r.Byte();
    }

    #endregion
}
