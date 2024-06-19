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
        InitTransfer();
        World.Hand = this;
    }

    private void Start() => Boss(() => true, 65f, 0);

    private void Update()
    {
        if (IsOwner || Dead) return;
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
        base.Write(w);
        w.Byte(HandPos);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        HandPos = r.Byte();
    }

    #endregion
}
