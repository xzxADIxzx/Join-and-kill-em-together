namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of both encounters with Gabriel. </summary>
public class Gabriel : Enemy
{
    /// <summary> Id of the current attack. </summary>
    private byte attack, lastAttack;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId), true);
        InitTransfer(() => Cooldown(IsOwner ? 0f : 4200f));
        Gabriel1 = GetComponent<global::Gabriel>();
        Gabriel2 = GetComponent<GabrielSecond>();
    }

    private void Start()
    {
        SpawnEffect();
        Boss(() => Tools.Scene == "Level 3-2" || Tools.Scene == "Level 6-2", 100f, 2);

        if (Gabriel1) Gabriel1.phaseChangeHealth = EnemyId.machine.health / 2f;
        if (Gabriel2) Gabriel2.phaseChangeHealth = EnemyId.machine.health / 2f;
    }

    private void Update()
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));

        if (lastAttack != attack)
        {
            if (Gabriel1) switch (lastAttack = attack)
                {
                    case 1: Tools.Invoke("AxeThrow", Gabriel1); break;
                    case 2: Tools.Invoke("SpearCombo", Gabriel1); break;
                    case 3: Tools.Invoke("StingerCombo", Gabriel1); break;
                    case 4: Tools.Invoke("ZweiDash", Gabriel1); break;
                    case 5: Tools.Invoke("ZweiCombo", Gabriel1); break;
                    default: Cooldown(4200f); break;
                }
        }
    }

    private void Cooldown(float time)
    {
        if (Gabriel1) Tools.Field<global::Gabriel>("attackCooldown").SetValue(Gabriel1, time);
        if (Gabriel2) Tools.Field<GabrielSecond>("attackCooldown").SetValue(Gabriel2, time);
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        w.Vector(transform.position);

        if (Gabriel1) w.Byte(Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name switch
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
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        attack = r.Byte();
    }

    public override void OnDied()
    {
        base.OnDied();
        if (Tools.Scene == "Level 3-2")
        {
            var parent = Tools.ObjFind("GabrielOutroParent").transform;
            var outro = parent.Find("GabrielOutro").GetComponent<GabrielOutro>();

            outro.SetSource(transform);
            outro.gabe = Gabriel1;
            outro.gameObject.SetActive(true);
            gameObject.SetActive(false);

            Tools.ObjFind("Music 3").SetActive(false);
            Tools.ObjFind("Eyeball").GetComponent<AlwaysLookAtCamera>().ChangeOverrideTarget(parent.Find("gab_Intro4"));
            StatsManager.Instance.StopTimer();
        }
    }

    #endregion
}
