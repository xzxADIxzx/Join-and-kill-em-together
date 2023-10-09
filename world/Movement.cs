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
    /// <summary> Environmental mask needed to prevent the skateboard from riding on water. </summary>
    private static int environmentMask = LayerMaskDefaults.Get(LMD.Environment);

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
        EmojiBind ??= UKAPI.GetKeyBind("EMOJI WHEEL", KeyCode.B);

        // if the emoji wheel is invisible and the key has been pressed for 0.25 seconds, then show it
        if (!EmojiWheel.Instance.Shown && EmojiBind.HoldTime > .25f) EmojiWheel.Instance.Show();

        // if the emoji wheel is visible, but the key is not pressed, then hide it
        if (EmojiWheel.Instance.Shown && !EmojiBind.IsPressedInScene) EmojiWheel.Instance.Hide();

        // skateboard logic
        if (Emoji == 0x0B)
        {
            // move the skateboard forward
            var player = NewMovement.Instance.transform;
            var target = player.forward * 20f;

            target.y = rb.velocity.y;
            rb.velocity = target;

            // don’t let the front and rear wheels fall into the ground
            if (Physics.Raycast(player.position + player.forward * 1.2f, Vector3.down, out var hit, 1.5f, environmentMask) && hit.distance > .8f)
                player.position = new(player.position.x, hit.point.y + 1.5f, player.position.z);

            if (Physics.Raycast(player.position - player.forward * 1.2f, Vector3.down, out var hit2, 1.5f, environmentMask) && hit2.distance > .8f)
                player.position = new(player.position.x, hit2.point.y + 1.5f, player.position.z);

            // turn to the sides
            if (!Chat.Instance.Shown)
            {
                float dir = InputManager.Instance.InputSource.Move.ReadValue<Vector2>().x;
                player.Rotate(new(0f, dir * 120f * Time.deltaTime, 0f));
            }
        }

        // third person camera
        if (Emoji != 0xFF)
        {
            // cancel animation if space is pressed
            if (Input.GetKey(KeyCode.Space) && !Chat.Instance.Shown) StartEmoji(0xFF);

            // rotate the camera according to mouse sensitivity
            if (!Chat.Instance.Shown && !LobbyTab.Instance.Shown && !PlayerList.Instance.Shown) // TODO replace with UIB.Shown
            {
                rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * OptionsManager.Instance.mouseSensitivity / 10f;
                rotation.y = Mathf.Clamp(rotation.y, 5f, 170f);
            }

            // turn on gravity, because if the taunt was launched on the ground, then it is disabled by default
            rb.useGravity = true;

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


        // ultrasoap
        if (SceneHelper.CurrentScene != "Main Menu" && !NewMovement.Instance.dead)
        {
            rb.constraints = Chat.Instance.Shown || OptionsManager.Instance.paused || Console.IsOpen
                ? RigidbodyConstraints.FreezeAll
                : Instance.Emoji == 0xFF || Instance.Emoji == 0x0B // skateboard
                    ? RigidbodyConstraints.FreezeRotation
                    : (RigidbodyConstraints)122;
        }


        // all the following changes are related to the network part of the game and shouldn't affect the local
        if (LobbyController.Lobby == null) return;

        // pause stops time and weapon wheel slows it down, but in multiplayer everything should be real-time
        Time.timeScale = 1f;

        // reset slam force if the player is riding on a rocket
        if (NewMovement.Instance.ridingRocket != null) NewMovement.Instance.slamForce = 0f;
    }

    #region toggling

    /// <summary> Updates the state machine: toggles movement, cursor and third-person camera. </summary>
    public static void UpdateState()
    {
        ToggleMovement(!Chat.Instance.Shown && Instance.Emoji == 0xFF);
        ToggleCursor(Chat.Instance.Shown || LobbyTab.Instance.Shown || PlayerList.Instance.Shown);
        ToggleHud(Instance.Emoji == 0xFF);

        // block camera rotation
        CameraController.Instance.enabled = CameraController.Instance.activated =
            !Chat.Instance.Shown && !LobbyTab.Instance.Shown && !PlayerList.Instance.Shown && Instance.Emoji == 0xFF;
    }

    /// <summary> Toggles the ability to move, used in the chat and etc. </summary>
    public static void ToggleMovement(bool enable)
    {
        NewMovement.Instance.enabled = GunControl.Instance.enabled = FistControl.Instance.enabled =
        FistControl.Instance.activated = HookArm.Instance.enabled = enable;

        // put the hook back in place
        if (!enable) HookArm.Instance.Cancel();
    }

    /// <summary> Toggles cursor visibility. </summary>
    public static void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary> Toggles hud visibility. </summary>
    public static void ToggleHud(bool enable)
    {
        // hide hud, weapons and arms
        StyleHUD.Instance.transform.parent.gameObject.SetActive(enable);
        GunControl.Instance.gameObject.SetActive(enable);
        FistControl.Instance.gameObject.SetActive(enable);

        // preventing some ultra stupid bug
        OptionsManager.Instance.frozen = !enable;
        Console.Instance.enabled = enable;
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
        var mat1 = EmojiPreview.transform.GetChild(0).GetChild(4).GetComponent<Renderer>().materials[1];
        var mat2 = EmojiPreview.transform.GetChild(0).GetChild(3).GetComponent<Renderer>().materials[0];

        mat1.mainTexture = mat2.mainTexture = DollAssets.WingTextures[(int)team];
        if (team == Team.Pink) EmojiPreview.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

        if (id == 6) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(7).gameObject.SetActive(true); // throne
        if (id == 8) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(6).GetChild(10).GetChild(0).localEulerAngles = new(-20f, 0f, 0f); // neck
    }

    /// <summary> Triggers an emoji with the given id. </summary>
    public void StartEmoji(byte id)
    {
        EmojiStart = Time.time;
        Emoji = id; // save id for synchronization over the network

        // toggle movement and third-person camera
        UpdateState();

        // destroy the old preview so they don't stack
        Destroy(EmojiPreview);

        // if id is -1, then the emotion was not selected
        if (id == 0xFF) return;
        else PreviewEmoji(id);

        // telling how to interrupt an emotion
        HudMessageReceiver.Instance.SendHudMessage("Press <color=orange>Space</color> to interrupt the emotion", silent: true);

        // stop sliding so that the preview is not underground
        NewMovement.Instance.playerCollider.height = 3.5f;
        NewMovement.Instance.gc.transform.localPosition = new(0f, -1.256f, 0f);

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
