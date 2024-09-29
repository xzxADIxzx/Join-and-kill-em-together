namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

using static Tools;

/// <summary> Representation of both encounters with Gabriel. </summary>
public class Gabriel : Enemy
{
    global::Gabriel gabriel1;
    GabrielSecond gabriel2;

    /// <summary> Id of the current attack. </summary>
    private byte attack, lastAttack;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId), true);
        InitTransfer(() => Cooldown(IsOwner ? 0f : 4200f));
        TryGetComponent(out gabriel1);
        TryGetComponent(out gabriel2);
    }

    private void Start()
    {
        SpawnEffect();
        Boss(Scene == "Level 3-2" || Scene == "Level 6-2", 100f, 2);

        if (gabriel1) gabriel1.phaseChangeHealth = EnemyId.machine.health / 2f;
        if (gabriel2) gabriel2.phaseChangeHealth = EnemyId.machine.health / 2f;

        if (gabriel2 && Scene == "Level 6-2")
        {
            var root = ObjFind("GabrielOutroParent").transform.parent;
            GameObject Find(string name) => root.Find(name).gameObject;

            gabriel2.onFirstPhaseEnd.toActivateObjects = new[] { Find("HatredColors/Lighting (DarkerFade)"), Find("FogDisabler") };
            gabriel2.onFirstPhaseEnd.toDisActivateObjects = new[] { Find("HatredColors/Lighting (Darker)") };
            gabriel2.onSecondPhaseStart.toActivateObjects = new[] { Find("EcstasyColors") };
            gabriel2.onSecondPhaseStart.toDisActivateObjects = new[] { Find("HatredColors") };
        }
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));

        if (lastAttack != attack)
        {
            if (gabriel1) switch (lastAttack = attack)
                {
                    case 1: Call("AxeThrow", gabriel1); break;
                    case 2: Call("SpearCombo", gabriel1); break;
                    case 3: Call("StingerCombo", gabriel1); break;
                    case 4: Call("ZweiDash", gabriel1); break;
                    case 5: Call("ZweiCombo", gabriel1); break;
                    default: Cooldown(4200f); break;
                }
            if (gabriel2) switch (lastAttack = attack)
                {
                    case 1: Call("CombineSwords", gabriel2); break;
                    case 2: Call("FastComboDash", gabriel2); break;
                    case 3: Call("BasicCombo", gabriel2); break;
                    case 4: Call("ThrowCombo", gabriel2); break;
                    case 5: Call("FastCombo", gabriel2); break;
                    default: Cooldown(4200f); break;
                }
        }
    });

    private void Cooldown(float time)
    {
        if (gabriel1) Set("attackCooldown", gabriel1, time);
        if (gabriel2) Set("attackCooldown", gabriel2, time);
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        if (gabriel1) w.Byte(Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name switch
        {
            "AxeThrow" => 1,
            "SpearReady" or
            "SpearDown" or
            "SpearThrow" => 2,
            "StingerCombo" => 3,
            "ZweiDash" => 4,
            "ZweiCombo" => 5,
            _ => 255
        });
        if (gabriel2) w.Byte(Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name switch
        {
            "SwordsCombine" or
            "SwordsCombinedThrow" => 1,
            "FastComboDash" => 2,
            "BasicCombo" => 3,
            "ThrowCombo" => 4,
            "FastCombo" => 5,
            _ => 255
        });
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
        attack = r.Byte();
    }

    public override void OnDied()
    {
        base.OnDied();
        bool l3 = Scene == "Level 3-2", l6 = Scene == "Level 6-2";

        if (l3 || l6)
        {
            var parent = ObjFind("GabrielOutroParent").transform;
            var outro = parent.Find("GabrielOutro").GetComponent<GabrielOutro>();

            outro.SetSource(transform);
            outro.gabe = gabriel1;
            outro.gabe2 = gabriel2;
            outro.gameObject.SetActive(true);
            gameObject.SetActive(false);

            if (l3)
            {
                ObjFind("Music 3").SetActive(false);
                ObjFind("Eyeball").GetComponent<AlwaysLookAtCamera>().ChangeOverrideTarget(parent.Find("gab_Intro4"));
            }
            if (l6)
            {
                ObjFind("BossMusic").SetActive(false);

                outro.GetComponent<Animator>().speed = .666f; // w-what?
                parent.parent.Find("EcstasyColors").gameObject.SetActive(false);
                parent.parent.Find("OutroLight").gameObject.SetActive(true);
            }
            StatsManager.Instance.StopTimer();
        }
    }

    #endregion
}
