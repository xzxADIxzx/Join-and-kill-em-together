namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Representation of Leviathan. </summary>
public class Leviathan : Enemy
{
    LeviathanController leviathan;
    LeviathanHead head => leviathan.head;
    LeviathanTail tail => leviathan.tail;

    /// <summary> Head and tail of Leviathan have a list of possible positions. </summary>
    public byte HeadPos, LastHeadPos, TailPos, LastTailPos, Attack, LastAttack;
    /// <summary> Whether the tail attacks from below or above. </summary>
    public bool TailFlag;

    private void Awake()
    {
        Init(_ => EntityType.Leviathan);
        TryGetComponent(out leviathan);

        Owner = LobbyController.LastOwner.AccountId;
        World.Leviathan = this;
    }

    private void Start()
    {
        Boss(true, 200f, 2);

        leviathan.phaseChangeHealth = EnemyId.statue.health / 2f;
        leviathan.active = IsOwner;

        if (!IsOwner) Cooldown(4200f);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;

        if (LastHeadPos != HeadPos)
        {
            if ((LastHeadPos = HeadPos) == byte.MaxValue)
                head.Invoke("Descend", 0f);
            else
            {
                var old = head.spawnPositions;
                head.spawnPositions = new[] { old[HeadPos] };
                head.ChangePosition();
                head.spawnPositions = old;

                Cooldown(4200f);
            }
        }
        if (LastTailPos != TailPos)
        {
            var old = tail.spawnPositions;
            tail.spawnPositions = new[] { old[LastTailPos = TailPos] };
            tail.ChangePosition();
            tail.spawnPositions = old;

            tail.transform.localPosition = old[TailPos] + (TailFlag ? Vector3.down * 30.5f : Vector3.down * 4.5f);
            tail.transform.localScale = new(TailFlag ? -1f : 1f, 1f, 1f);
        }
        if (LastAttack != Attack)
        {
            switch (LastAttack = Attack)
            {
                case 0: Tools.Invoke("ProjectileBurst", head); break;
                case 1: Tools.Invoke("Bite", head); break;
            }
            Cooldown(4200f);
        }
    });

    private void Cooldown(float time) => Tools.Set("attackCooldown", head, time);

    #region entity

    public override void Write(Writer w)
    {
        UpdatesCount++;

        w.Byte(HeadPos);
        w.Byte(TailPos);
        w.Byte(Attack);
        w.Bool(leviathan.tail.transform.localScale.x == -1f);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        HeadPos = r.Byte();
        TailPos = r.Byte();
        Attack = r.Byte();
        TailFlag = r.Bool();
    }

    #endregion
}
