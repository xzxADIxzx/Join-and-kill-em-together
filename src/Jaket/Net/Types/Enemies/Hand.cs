namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Representation of Minos' hand. </summary>
public class Hand : Enemy
{
    /// <summary> Hand position used to synchronize attacks. </summary>
    public byte HandPos = 0xFF, LastHandPos = 0xFF;

    private void Awake()
    {
        Init(_ => EntityType.Hand, true);

        Owner = LobbyController.LastOwner.AccountId;
        World.Hand = this;
    }

    private void Start() => Boss(true, 65f, 0);

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        if (LastHandPos != HandPos) Animator.SetTrigger((LastHandPos = HandPos) switch
        {
            0 => "SlamDown",
            1 => "SlamLeft",
            2 => "SlamRight",
            _ => ""
        });
    });

    #region entity

    public override void Write(Writer w)
    {
        UpdatesCount++;
        w.Byte(HandPos);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;
        HandPos = r.Byte();
    }

    #endregion
}
