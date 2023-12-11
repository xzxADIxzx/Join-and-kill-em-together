namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Minos hand representation responsible for synchronizing animations and attacks. </summary>
public class Hand : Entity
{
    /// <summary> Minos hand health. </summary>
    private FloatLerp health = new();
    /// <summary> Hand position used to synchronize attacks. </summary>
    public byte HandPos = 0xFF, LastHandPos = 0xFF;

    private void Awake()
    {
        Init(_ => EntityType.Hand);

        if (LobbyController.IsOwner)
            LobbyController.ScaleHealth(ref EnemyId.statue.health);
        else
            GetComponent<MinosArm>().enabled = false; // artificial intelligence of the hand is not needed by clients

        World.Instance.Hand = this;
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        EnemyId.health = EnemyId.statue.health = health.Get(LastUpdate);
        EnemyId.dead = EnemyId.health <= 0f;

        if (LastHandPos != HandPos) Animator.SetTrigger((LastHandPos = HandPos) switch
        {
            0 => "SlamDown",
            1 => "SlamLeft",
            2 => "SlamRight",
            _ => ""
        });
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Float(EnemyId.health);
        w.Byte(HandPos);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        HandPos = r.Byte();
    }

    #endregion
}
