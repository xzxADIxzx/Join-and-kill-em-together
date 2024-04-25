namespace Jaket.Net.Types.Players;

using UnityEngine;

using Jaket.IO;

/// <summary>
/// Doll of a player, remote from network or local from emoji.
/// Responsible for the visual part of the player, i.e. suits and animations.
/// </summary>
public class Doll
{
    /// <summary> Component rendering animations of the doll. </summary>
    public Animator Animator;
    /// <summary> Animator states that affect which animation will be played. </summary>
    public bool Walking, Sliding, Falling, InAir, WasInAir, Dashing, WasDashing, Riding, WasRiding, Hooking, WasHooking, Shopping, WasShopping;

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
    }

    #region entity

    public void WriteAnim(Writer w) => w.Bools(Walking, Sliding, Falling, InAir, Dashing, Riding, Hooking, Shopping);

    public void ReadAnim(Reader r) => r.Bools(out Walking, out Sliding, out Falling, out InAir, out Dashing, out Riding, out Hooking, out Shopping);

    #endregion
}
