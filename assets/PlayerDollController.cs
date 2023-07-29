namespace Jaket.Assets;

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
        if (Input.GetKeyDown(KeyCode.F)) animator.SetTrigger("Punch");

        animator.SetBool("Walking", Input.GetKey(KeyCode.W));
        animator.SetBool("Sliding", Input.GetKey(KeyCode.LeftControl));
        animator.SetBool("InAir", Input.GetKey(KeyCode.Space));
    }
}
