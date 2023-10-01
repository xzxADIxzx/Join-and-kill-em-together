namespace Jaket.World;

using GameConsole;
using System.Collections;
using UMM;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;

/// <summary> Class responsible for additions to control and local display of emotions. </summary>
public class Movement : MonoSingleton<Movement>
{
    /// <summary> Reference to local player's rigidbody. </summary>
    private static Rigidbody rb { get => NewMovement.Instance.rb; }

    /// <summary> Emoji selection wheel keybind. </summary>
    public UKKeyBind EmojiBind;
    /// <summary> Current emotion preview, can be null. </summary>
    public GameObject EmojiPreview;
    /// <summary> An array containing the length of all emotions in seconds. </summary>
    public float[] EmojiLegnth = { 2.458f, 4.708f, 1.833f, 2.875f, 0f, 9.083f, -1f, 12.125f, -1f, 3.292f, 0f, -1f };
    /// <summary> Start time of the current emotion. </summary>
    public float EmojiStart;
    /// <summary> Id of the currently playing emoji. </summary>
    public byte Emoji = 0xFF, Rps;

    /// <summary> Starting and ending position of third person camera. </summary>
    private readonly Vector3 start = new(0f, 6f, 0f), end = new(0f, .1f, 0f);
    /// <summary> Third person camera position. </summary>
    private Vector3 position;
    /// <summary> Third person camera rotation. </summary>
    private Vector2 rotation;

    /// <summary> Creates a singleton of movement. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("Movement", Plugin.Instance.transform).AddComponent<Movement>();
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
            // cancel animation if space is pressed
            if (Input.GetKey(KeyCode.Space) && !Chat.Instance.Shown) StartEmoji(0xFF);

            // rotate the camera according to mouse sensitivity
            rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * OptionsManager.Instance.mouseSensitivity / 10f;
            rotation.y = Mathf.Clamp(rotation.y, 5f, 170f);

            var cam = CameraController.Instance.cam.transform;
            var player = NewMovement.Instance.transform.position + new Vector3(0f, 1f, 0f);

            // move the camera position towards the start if the animation has just started, or towards the end if the animation ends
            bool ends = Emoji == 0xFF || (Time.time - EmojiStart > EmojiLegnth[Emoji] && EmojiLegnth[Emoji] != -1f);
            position = Vector3.MoveTowards(position, ends ? end : start, 12f * Time.deltaTime);

            // return the camera to its original position
            cam.position = player + position;

            // rotate the camera around the player
            cam.RotateAround(player, Vector3.left, rotation.y);
            cam.RotateAround(player, Vector3.up, rotation.x);
            cam.LookAt(player);

            // do not let the camera fall through the ground
            if (Physics.SphereCast(player, .25f, cam.position - player, out var hit, position.magnitude, LayerMaskDefaults.Get(LMD.Environment)))
                cam.position = hit.point + .5f * hit.normal;
        }


        // all the following changes are related to the network part of the game and shouldn't affect the local
        if (LobbyController.Lobby == null) return;

        // pause stops time and weapon wheel slows it down, but in multiplayer everything should be real-time
        Time.timeScale = 1f;

        // sometimes it happens that in the chat the player flies into the air
        if (!NewMovement.Instance.dead) rb.constraints = NewMovement.Instance.enabled ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;

        // reset slam force if the player is riding on a rocket
        if (NewMovement.Instance.ridingRocket != null) NewMovement.Instance.slamForce = 0f;
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
        Console.Instance.enabled = enable;

        // block camera rotation
        CameraController.Instance.enabled = CameraController.Instance.activated = enable;
    }

    #endregion
    #region emoji

    /// <summary> Creates a preview of the given emoji in player coordinates. </summary>
    public void PreviewEmoji(byte id)
    {
        EmojiPreview = Instantiate(DollAssets.Preview, NewMovement.Instance.transform);
        EmojiPreview.transform.localPosition = new(0f, -1.5f, 0f);
        EmojiPreview.transform.localScale = new(2.18f, 2.18f, 2.18f); // preview created for terminal and too small

        var anim = EmojiPreview.transform.GetChild(0).GetComponent<Animator>();

        anim.SetTrigger("Show Emoji");
        anim.SetInteger("Emoji", id);
        anim.SetInteger("Rps", Rps);

        // apply team to emotion preview
        var team = Networking.LocalPlayer.team;
        var mat = EmojiPreview.transform.GetChild(0).GetChild(4).GetComponent<Renderer>().materials[1];

        mat.mainTexture = DollAssets.WingTextures[(int)team];
        mat.color = team.Data().WingColor();
        if (team == Team.Pink) EmojiPreview.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

        if (id == 6) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(7).gameObject.SetActive(true); // throne
        if (id == 8) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(6).GetChild(10).GetChild(0).localEulerAngles = new(-20f, 0f, 0f); // neck
    }

    /// <summary> Triggers an emoji with the given id. </summary>
    public void StartEmoji(byte id)
    {
        EmojiStart = Time.time;
        Emoji = id; // save id for synchronization over the network

        ToggleCamera(Emoji == 0xFF);
        if (!Chat.Instance.Shown) ToggleMovement(Emoji == 0xFF);

        // destroy the old preview so they don't stack
        Destroy(EmojiPreview);

        // if id is -1, then the emotion was not selected
        if (id == 0xFF) return;
        else PreviewEmoji(id);

        // telling how to interrupt an emotion
        HudMessageReceiver.Instance.SendHudMessage("Press <color=orange>Space</color> to interrupt the emotion", silent: true);

        // rotate the third person camera in the same direction as the first person camera
        rotation = new(CameraController.Instance.rotationY, CameraController.Instance.rotationX + 90f);
        position = new();

        StopCoroutine("ClearEmoji");
        if (EmojiLegnth[id] != -1f) StartCoroutine("ClearEmoji");
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
