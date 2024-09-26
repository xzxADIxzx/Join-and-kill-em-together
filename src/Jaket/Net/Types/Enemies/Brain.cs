namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Representation of Earthmover's brain. </summary>
public class Brain : Enemy
{
    /// <summary> Whether the player is currently fighting the brain. </summary>
    public bool IsFightActive;

    /// <summary> Idols' state. </summary>
    private bool idol1, idol2;
    /// <summary> Idols' doors. </summary>
    private Door door1, door2;

    private void Awake()
    {
        Init(_ => EntityType.Brain);

        Owner = LobbyController.LastOwner.AccountId;
        World.Brain = this;
    }

    private void Start()
    {
        Boss(true, 100f, 1);

        transform.parent.Find("IdolPod/Cylinder").TryGetComponent(out door1);
        transform.parent.Find("IdolPod (1)/Cylinder").TryGetComponent(out door2);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead || !IsFightActive) return;

        if (door1.open && !idol1) door1.Close();
        if (door2.open && !idol2) door2.Close();

        if (!door1.open && idol1) door1.Open();
        if (!door2.open && idol2) door2.Open();
    });

    #region entity

    public override void Write(Writer w)
    {
        UpdatesCount++;

        w.Bool(door1.open);
        w.Bool(door2.open);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        idol1 = r.Bool();
        idol2 = r.Bool();
    }

    #endregion
}
