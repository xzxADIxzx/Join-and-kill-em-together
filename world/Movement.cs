namespace Jaket.World;

using System.Collections;
using UMM;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Assets;
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
    /// <summary> Current emotion preview, can be null. </summary>
    public GameObject EmojiPreview;
    /// <summary> An array containing the length of all emotions in seconds. </summary>
    public float[] EmojiLegnth = { 2.458f, 4.708f, 1.833f, 3.292f, 12.125f, 9.083f };
    /// <summary> Id of the currently playing emoji. </summary>
    public byte Emoji = 0xFF;

    /// <summary> Starting position of third person camera. </summary>
    private Vector3 start = new(0f, 6f, 0f);
    /// <summary> Third person camera rotation. </summary>
    private Vector2 rotation;

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
        if (EmojiBind == null) EmojiBind = UKAPI.GetKeyBind("EMOJI WHEEL", KeyCode.B);

        // if the emoji wheel is invisible and the key has been pressed for 0.25 seconds, then show it
        if (!EmojiWheel.Instance.Shown && EmojiBind.HoldTime > .25f) EmojiWheel.Instance.Show();

        // if the emoji wheel is visible, but the key is not pressed, then hide it
        if (EmojiWheel.Instance.Shown && !EmojiBind.IsPressedInScene) EmojiWheel.Instance.Hide();

        // third person camera
        if (Emoji != 0xFF)
        {
            // cancel animation if any key is pressed except return
            if (Input.anyKey && !Chat.Instance.Shown && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter)) StartEmoji(0xFF);

            // rotate the camera according to mouse sensitivity
            rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * OptionsManager.Instance.mouseSensitivity / 10f;
            rotation.y = Mathf.Clamp(rotation.y, 5f, 170f);

            var cam = CameraController.Instance.cam.transform;
            var player = NewMovement.Instance.transform.position + new Vector3(0f, 1f, 0f);

            // return the camera to its original position
            cam.position = player + start;

            // rotate the camera around the player
            cam.RotateAround(player, Vector3.left, rotation.y);
            cam.RotateAround(player, Vector3.up, rotation.x);
            cam.LookAt(player);
        }


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

    /// <summary> Toggles the ability to rotate the camera and hud. </summary>
    public static void ToggleCamera(bool enable)
    {
        // hide hud, weapons and arms
        StyleHUD.Instance.transform.parent.gameObject.SetActive(enable);
        GunControl.Instance.gameObject.SetActive(enable);
        FistControl.Instance.gameObject.SetActive(enable);

        // preventing some ultra stupid bug
        OptionsManager.Instance.frozen = !enable;

        // block camera rotation
        CameraController.Instance.enabled = enable;
    }

    #endregion
    #region emoji

    /// <summary> Creates a preview of the given emoji in player coordinates. </summary>
    public void PreviewEmoji(byte id)
    {
        EmojiPreview = Instantiate(DollAssets.Preview, NewMovement.Instance.transform.position - new Vector3(0f, 1.5f, 0f), NewMovement.Instance.transform.rotation);
        EmojiPreview.transform.localScale = new(2.18f, 2.18f, 2.18f); // preview created for terminal and too small

        var anim = EmojiPreview.transform.GetChild(0).GetComponent<Animator>();

        anim.SetTrigger("Show Emoji");
        anim.SetInteger("Emoji", id);
    }

    /// <summary> Triggers an emoji with the given id. </summary>
    public void StartEmoji(byte id)
    {
        // save id for synchronization over the network
        Emoji = id;
        ToggleCamera(Emoji == 0xFF);

        // if id is -1, then the emotion was not selected
        if (id == 0xFF)
        {
            Destroy(EmojiPreview);
            return;
        }
        else PreviewEmoji(id);

        // rotate the third person camera in the same direction as the first person camera
        rotation = new(CameraController.Instance.rotationY, CameraController.Instance.rotationX + 90f);

        StopCoroutine("ClearEmoji");
        StartCoroutine("ClearEmoji");
    }

    /// <summary> Returns the emoji id to -1 after the end of an animation. </summary>
    public IEnumerator ClearEmoji()
    {
        // wait for the end of an animation
        yield return new WaitForSeconds(EmojiLegnth[Emoji] + .5f);

        // return the emoji id to -1
        StartEmoji(0xFF);
    }

    #endregion
}
