namespace Jaket.World;

using System.Collections;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net;
using Jaket.UI;

/// <summary> Class responsible for additions to control and local display of emotions. </summary>
public class Movement : MonoSingleton<Movement>
{
    /// <summary> Reference to local player's rigidbody. </summary>
    private static Rigidbody rb { get => NewMovement.Instance.rb; }
    /// <summary> Whether cheats were enabled at the time of blocking movement. </summary>
    private static bool wasCheatsEnabled;

    /// <summary> Emoji selection wheel keybind. </summary>
    public UKKeyBind EmojiBind;
    /// <summary> An array containing the length of all emotions in seconds. </summary>
    public float[] EmojiLegnth = { 2.458f, 0f, 1.833f, 0f, 0f, 0f };
    /// <summary> Id of the currently playing emoji. </summary>
    public byte Emoji = 0xFF;

    /// <summary> Creates a singleton of movement. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("Movement", Plugin.Instance.transform).AddComponent<Movement>();

        // don't need to save cheat state between levels
        SceneManager.sceneLoaded += (scene, mode) => wasCheatsEnabled = false;
    }

    public void LateUpdate() // late update is needed in order to overwrite the time scale value
    {
        // find or create a keybind if it doesn't already exist
        if (EmojiBind == null) EmojiBind = UKAPI.GetKeyBind("EMOJI WHEEL", KeyCode.G);

        // if the emoji wheel is invisible and the key has been pressed for 0.25 seconds, then show it
        if (!EmojiWheel.Instance.Shown && EmojiBind.HoldTime > .25f) EmojiWheel.Instance.Show();

        // if the emoji wheel is visible, but the key is not pressed, then hide it
        if (EmojiWheel.Instance.Shown && !EmojiBind.IsPressedInScene) EmojiWheel.Instance.Hide();


        // all the following changes are related to the network part of the game and shouldn't affect the local
        if (LobbyController.Lobby == null) return;

        // pause stops time and weapon wheel slows it down, but in multiplayer everything should be real-time
        if (OptionsManager.Instance.paused || WeaponWheel.Instance.gameObject.activeSelf) Time.timeScale = 1f;

        // sometimes it happens that in the chat the player flies into the air
        if (!NewMovement.Instance.dead) rb.constraints = NewMovement.Instance.enabled ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
    }

    #region toggling

    /// <summary> Toggles the ability to move, used in the chat and etc. </summary>
    public static void ToggleMovement(bool enable)
    {
        NewMovement.Instance.enabled = GunControl.Instance.enabled = FistControl.Instance.enabled = HookArm.Instance.enabled = enable;

        // put the hook back in place
        if (!enable) HookArm.Instance.Cancel();

        // fix ultrasoap
        rb.constraints = enable ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;

        // temporary disable cheats
        if (enable)
            CheatsController.Instance.cheatsEnabled = wasCheatsEnabled;
        else
        {
            wasCheatsEnabled = CheatsController.Instance.cheatsEnabled;
            CheatsController.Instance.cheatsEnabled = false;
        }
    }

    /// <summary> Toggles cursor visibility. </summary>
    public static void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;

        // block camera rotation
        CameraController.Instance.enabled = !enable;
    }

    #endregion
    #region emoji

    /// <summary> Triggers an emoji with the given id. </summary>
    public void StartEmoji(byte id)
    {
        Emoji = id;

        StopCoroutine("ClearEmoji");
        StartCoroutine("ClearEmoji");
    }

    /// <summary> Returns the emoji id to -1 after the end of an animation. </summary>
    public IEnumerator ClearEmoji()
    {
        // wait for the end of an animation
        yield return new WaitForSeconds(EmojiLegnth[Emoji]);

        // return the emoji id to -1
        Emoji = 0xFF;
    }

    #endregion
}
