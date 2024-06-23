namespace Jaket.Net.Types;

using UnityEngine;
using UnityEngine.AI;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a spider body enemy??? Who da hell gave this thing such name?! </summary>
public class Body : Enemy
{
    /// <summary> Whether the malicious face is charging a shot. </summary>
    private bool charging, lastCharging;
    /// <summary> Whether the corpse must be broken at the next <i>death</i>. </summary>
    private bool toBreakCorpse;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer(() => Cooldown(IsOwner ? 0f : -4200f));
    }

    private void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 0-1", 25f);

        transform.parent.position = transform.position + Vector3.down * 10f; // teleport the spawn effect
        Events.Post2(() =>
        {
            // sometimes (very rarely) navigation agent prevents the spider from teleporting to the synced position
            GetComponent<NavMeshAgent>().Warp(transform.position);

            // move the spider's legs at least somewhere
            foreach (var leg in transform.parent.GetComponentsInChildren<SpiderLegPos>()) leg.Invoke("MoveLeg", 0f);
        });
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));

        if (lastCharging != charging && (lastCharging = charging)) EnemyId.spider.Invoke("ChargeBeam", 0f);
    });

    private void Cooldown(float time) => Tools.Field<SpiderBody>("beamProbability").SetValue(EnemyId.spider, time);

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Bool(EnemyId.spider.currentCE != null);
        if (EnemyId.spider.currentCE == null) w.Vector(transform.position);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        charging = r.Bool();
        if (!charging)
        {
            x.Read(r); y.Read(r); z.Read(r);
        }
    }

    public override void Kill()
    {
        if (toBreakCorpse)
            EnemyId.spider.BreakCorpse();
        else
        {
            EnemyId.InstaKill();
            toBreakCorpse = true;
        }
    }

    #endregion
}
