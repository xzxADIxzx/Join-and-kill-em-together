namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Minos hand representation responsible for synchronizing animations and attacks. </summary>
public class Hand : Entity
{
    /// <summary> Enemy identifier component needed for health synchronization. </summary>
    private EnemyIdentifier enemyId;

    /// <summary> Minos hand health. </summary>
    private FloatLerp health;

    /// <summary> Animator is required to synchronize attacks, because they are launched through animations. </summary>
    private Animator animator;

    /// <summary> Hand position used to synchronize attacks. </summary>
    public byte HandPos = 0xFF, LastHandPos = 0xFF;

    private void Awake()
    {
        if (LobbyController.IsOwner)
        {
            Id = Entities.NextId();
            Type = EntityType.Hand;
        }

        health = new();
        enemyId = GetComponent<EnemyIdentifier>();
        animator = GetComponent<Animator>();

        // artificial intelligence of the hand is not needed by clients
        if (!LobbyController.IsOwner) GetComponent<MinosArm>().enabled = false;

        World.Instance.Hand = this;
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        enemyId.health = health.Get(LastUpdate);
        enemyId.dead = enemyId.health <= 0f;

        if (LastHandPos != HandPos) animator.SetTrigger((LastHandPos = HandPos) switch
        {
            0 => "SlamDown",
            1 => "SlamLeft",
            2 => "SlamRight",
            _ => ""
        });
    }

    public override void Write(Writer w)
    {
        w.Float(enemyId.health);
        w.Byte(HandPos);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        HandPos = r.Byte();
    }

    public override void Damage(Reader r) => Bullets.DealDamage(enemyId, r);
}
