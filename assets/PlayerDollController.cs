namespace Jaket.Assets;

using System.Collections;
using UnityEngine;

/// <summary> Class needed for testing animations. </summary>
public class PlayerDollController : MonoBehaviour
{
    /// <summary> Animator containing animations to be tested. </summary>
    private Animator animator;

    private void Awake() => animator = GetComponent<Animator>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) animator.SetTrigger("Jump");
        if (Input.GetKeyDown(KeyCode.LeftShift)) animator.SetTrigger("Dash");
        if (Input.GetKeyDown(KeyCode.C)) animator.SetTrigger("Ride");
        if (Input.GetKeyDown(KeyCode.F)) animator.SetTrigger("Punch");
        if (Input.GetKeyDown(KeyCode.R)) animator.SetTrigger("Parry");
        if (Input.GetKeyDown(KeyCode.Mouse3)) animator.SetTrigger("Throw Hook");

        if (Input.GetKeyUp(KeyCode.Alpha1)) StartEmoji(0);
        if (Input.GetKeyUp(KeyCode.Alpha2)) StartEmoji(1);
        if (Input.GetKeyUp(KeyCode.Alpha3)) StartEmoji(2);
        if (Input.GetKeyUp(KeyCode.Alpha4)) StartEmoji(3);
        if (Input.GetKeyUp(KeyCode.Alpha5)) StartEmoji(4);
        if (Input.GetKeyUp(KeyCode.Alpha6)) StartEmoji(5);

        if (Input.anyKey) animator.SetInteger("Emoji", -1);

        animator.SetBool("Walking", Input.GetKey(KeyCode.W));
        animator.SetBool("Sliding", Input.GetKey(KeyCode.LeftControl));
        animator.SetBool("Dashing", Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("Riding", Input.GetKey(KeyCode.C));
        animator.SetBool("InAir", Input.GetKey(KeyCode.Space));
        animator.SetBool("UsingHook", Input.GetKey(KeyCode.Mouse3));
    }

    /// <summary> Triggers an emoji with the given id. </summary>
    private void StartEmoji(int id)
    {
        animator.SetTrigger("Show Emoji");
        animator.SetInteger("Emoji", id);

        StopCoroutine("ClearEmoji");
        StartCoroutine("ClearEmoji");
    }

    /// <summary> Returns the emoji id to -1 after the end of an animation. </summary>
    private IEnumerator ClearEmoji()
    {
        yield return new WaitForSeconds(animator.GetInteger("Emoji") == 0 ? 2.458f : 1.833f);
        animator.SetInteger("Emoji", -1);
    }
}
