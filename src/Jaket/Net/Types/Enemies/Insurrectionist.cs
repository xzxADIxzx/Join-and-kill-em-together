namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of an insurrectionist. </summary>
public class Insurrectionist : Enemy
{
    Sisyphus sisy;
    AudioSource audio;
    AudioClip stomp;
    AudioClip[] other;

    /// <summary> Whether the next insurrectionist will be an angry or rude. </summary>
    private static bool angryOrRude;
    /// <summary> Id of the current attack. </summary>
    private byte attack, lastAttack;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId), true);
        InitTransfer();
        TryGetComponent(out sisy);
        TryGetComponent(out audio);
        stomp = Tools.Get("stompVoice", sisy) as AudioClip;
        other = Tools.Get("attackVoices", sisy) as AudioClip[];
    }

    private void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 4-2", 110f, 1);
        Boss(Tools.Scene == "Level 6-1", 100f, 1, (angryOrRude = !angryOrRude) ? "INSURRECTIONIST \"ANGRY\"" : "INSURRECTIONIST \"RUDE\"");

        if (Tools.Scene == "Level 4-2") GetComponentsInChildren<SkinnedMeshRenderer>().Do(r => r.material.color = new(1f, .9f, .4f));
        if (Tools.Scene == "Level 6-1") GameAssets.SisyMaterial(angryOrRude ? "SisyphusRed" : "SisyphusBlue", GetComponentsInChildren<SkinnedMeshRenderer>());

        if (!IsOwner) Cooldown(4200f);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));

        if (lastAttack != attack)
        {
            switch (lastAttack = attack)
            {
                case 1:
                    Animator.SetTrigger("Stomp");
                    audio.PlayOneShot(stomp);
                    break;
                case 2:
                    Animator.SetTrigger("OverheadSlam");
                    SetTracking(1f, .15f);
                    break;
                case 3:
                    Animator.SetTrigger("HorizontalSwing");
                    SetTracking(0f, 1f);
                    break;
                case 4:
                    Animator.SetTrigger("Stab");
                    SetTracking(.9f, .5f);
                    break;
                case 5:
                    Animator.SetTrigger("AirStab");
                    SetTracking(.9f, .9f);
                    break;

                case 6: Tools.Invoke("Jump", sisy, true); break;
                default: sisy.Invoke("StopAction", 0.1f); break;
            }
            if (attack >= 2 && attack <= 5)
            {
                Tools.Set("m_AttackType", sisy, attack - 2);
                audio.PlayOneShot(other[attack - 2]);
            }
        }
    });

    private void Cooldown(float time) => Tools.Set("cooldown", sisy, time);

    private void SetTracking(float x, float y)
    {
        Tools.Set("trackingX", sisy, x);
        Tools.Set("trackingY", sisy, y);
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        w.Byte(Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name switch
        {
            "Stomp" => 1,
            "OverheadSlam" => 2,
            "HorizontalSwing" => 3,
            "Stab" => 4,
            "AirStab" => 5,
            "Jump" => 6,
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

    public override void Damage(Reader r)
    {
        base.Damage(r);
        if (EnemyId.hitter == "cannonball") sisy.Knockdown(transform.position + transform.forward);
    }

    #endregion
}
