namespace Jaket.Net.Types.Players;

using System;
using UnityEngine;

using Jaket.IO;

/// <summary>
/// Doll of a player, remote from network or local from emoji.
/// Responsible for the visual part of the player, i.e. suits and animations.
/// </summary>
public class Doll : MonoBehaviour
{
    /// <summary> Component rendering animations of the doll. </summary>
    public Animator Animator;
    /// <summary> Animator states that affect which animation will be played. </summary>
    public bool Walking, Sliding, Falling, InAir, WasInAir, Dashing, WasDashing, Riding, WasRiding, Hooking, WasHooking, Shopping, WasShopping;

    /// <summary> Emoji that plays at the moment. </summary>
    public byte Emoji, LastEmoji = 0xFF, Rps;
    /// <summary> Event triggered after the start of emoji. </summary>
    public Action OnEmojiStart = () => { };

    /// <summary> Slide and fall particles transforms. </summary>
    public Transform SlideParticle, FallParticle;

    private void Update()
    {
        Animator.SetBool("walking", Walking);
        Animator.SetBool("sliding", Sliding);
        Animator.SetBool("in-air", InAir);
        Animator.SetBool("dashing", Dashing);
        Animator.SetBool("riding", Riding);
        Animator.SetBool("hooking", Hooking);
        Animator.SetBool("shopping", Shopping);

        if (WasInAir != InAir && (WasInAir = InAir)) Animator.SetTrigger("jump");
        if (WasRiding != Riding && (WasRiding = Riding)) Animator.SetTrigger("ride");
        if (WasDashing != Dashing && (WasDashing = Dashing)) Animator.SetTrigger("dash");
        if (WasHooking != Hooking)
        {
            if (WasHooking = Hooking) Animator.SetTrigger("hook");
            // hook.gameObject.SetActive(Hooking);
        }
        if (WasShopping != Shopping && (WasShopping = Shopping)) Animator.SetTrigger("open-shop");

        if (LastEmoji != Emoji)
        {
            Animator.SetTrigger("show-emoji");
            Animator.SetInteger("emoji", LastEmoji = Emoji);
            Animator.SetInteger("rps", Rps);

            OnEmojiStart();
        }

        if (Sliding && SlideParticle == null)
        {
            SlideParticle = Instantiate(NewMovement.Instance.slideParticle, transform).transform;
            SlideParticle.localPosition = new(0f, 0f, 3.5f);
            SlideParticle.localEulerAngles = new(0f, 180f, 0f);
            SlideParticle.localScale = new(1.5f, 1f, .8f);
        }
        else if (!Sliding && SlideParticle != null) Destroy(SlideParticle.gameObject);

        if (Falling && FallParticle == null)
        {
            FallParticle = Instantiate(NewMovement.Instance.fallParticle, transform).transform;
            FallParticle.localPosition = new(0f, 6f, 0f);
            FallParticle.localEulerAngles = new(90f, 0f, 0f);
            FallParticle.localScale = new(1.2f, .6f, 1f);
        }
        else if (!Falling && FallParticle != null) Destroy(FallParticle.gameObject);
    }

    #region entity

    public void WriteAnim(Writer w) => w.Bools(Walking, Sliding, Falling, InAir, Dashing, Riding, Hooking, Shopping);

    public void ReadAnim(Reader r) => r.Bools(out Walking, out Sliding, out Falling, out InAir, out Dashing, out Riding, out Hooking, out Shopping);

    #endregion
}
