namespace Jaket.World;

using GameConsole;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.UI;
using Jaket.UI.Elements;

/// <summary> Class responsible for additions to control and local display of emotions. </summary>
public class Movement : MonoSingleton<Movement>
{
    private static NewMovement nm => NewMovement.Instance;
    private static FistControl fc => FistControl.Instance;
    private static CameraController cc => CameraController.Instance;

    /// <summary> Current emotion preview, can be null. </summary>
    public GameObject EmojiPreview;
    /// <summary> An array containing the length of all emotions in seconds. </summary>
    public float[] EmojiLegnth = { 2.458f, 4.708f, 1.833f, 2.875f, 0f, 9.083f, -1f, 12.125f, -1f, 3.292f, 0f, -1f };
    /// <summary> Start time of the current emotion and hold time of the emotion wheel key. </summary>
    public float EmojiStart, HoldTime;
    /// <summary> Id of the currently playing emoji. </summary>
    public byte Emoji = 0xFF, Rps;

    /// <summary> Starting and ending position of third person camera. </summary>
    private readonly Vector3 start = new(0f, 6f, 0f), end = new(0f, .1f, 0f);
    /// <summary> Third person camera position. </summary>
    private Vector3 position;
    /// <summary> Third person camera rotation. </summary>
    private Vector2 rotation;

    /// <summary> Environmental mask needed to prevent the skateboard from riding on water. </summary>
    private readonly int mask = LayerMaskDefaults.Get(LMD.Environment);
    /// <summary> Speed at which the skateboard moves. </summary>
    private float skateboardSpeed;
    /// <summary> When the maximum skateboard speed is exceeded, deceleration is activated. </summary>
    private bool slowsDown;
    /// <summary> Current falling particle object. </summary>
    private GameObject fallParticle;

    /// <summary> Last pointer created by the player. </summary>
    public Pointer Pointer;

    /// <summary> Creates a singleton of movement. </summary>
    public static void Load()
    {
        // initialize the singleton
        UI.Object("Movement").AddComponent<Movement>();

        // interrupt emoji to prevent some bugs
        Events.OnLoaded += () => Instance.StartEmoji(0xFF, false);

        // update death screen text to display number of living players in the Cyber Grind
        Instance.InvokeRepeating("DeathScreenUpdate", 0f, 1f);
    }

    private void Update()
    {
        // mod and game menus may conflict
        if (UI.AnyBuiltIn() || Settings.Instance.Rebinding) return;

        if (Input.GetKeyDown(Settings.LobbyTab)) LobbyTab.Instance.Toggle();
        if (Input.GetKeyDown(Settings.PlayerList)) PlayerList.Instance.Toggle();
        if (Input.GetKeyDown(Settings.Settingz)) Settings.Instance.Toggle();

        if (Input.GetKeyDown(Settings.PlayerIndicators)) PlayerIndicators.Instance.Toggle();
        if (Input.GetKeyDown(Settings.PlayerInfo)) PlayerInfo.Instance?.Toggle();

        if (Input.GetKeyDown(Settings.Chat)) Chat.Instance.Toggle();
        if (Input.GetKeyDown(Settings.ScrollUp)) Chat.Instance.ScrollMessages(true);
        if (Input.GetKeyDown(Settings.ScrollDown)) Chat.Instance.ScrollMessages(false);

        if (Input.GetKeyDown(Settings.Pointer) && Physics.Raycast(cc.transform.position, cc.transform.forward, out var hit, float.MaxValue, mask))
        {
            if (Pointer != null) Pointer.Lifetime = 4.5f;
            Pointer = Pointer.Spawn(Networking.LocalPlayer.Team, hit.point, hit.normal);

            if (LobbyController.Lobby != null) Networking.Send(PacketType.Point, w =>
            {
                w.Id(Networking.LocalPlayer.Id);
                w.Vector(hit.point);
                w.Vector(hit.normal);
            }, size: 32);
        }

        if (Input.GetKey(Settings.EmojiWheel))
        {
            HoldTime += Time.deltaTime; // if the key has been pressed for 0.25 seconds, show the emoji wheel
            if (!EmojiWheel.Shown && HoldTime > .25f) EmojiWheel.Instance.Show();
        }
        else
        {
            HoldTime = 0f;
            if (EmojiWheel.Shown) EmojiWheel.Instance.Hide();
        }

        if (Input.GetKeyDown(Settings.SelfDestruction) && !UI.AnyMovementBlocking()) nm.GetHurt(1000, false, 0f);
        if (Input.GetKeyDown(KeyCode.F11)) InteractiveGuide.Instance.Launch();
    }

    private void LateUpdate() // late update is needed to overwrite the time scale value and camera rotation
    {
        // skateboard logic
        Skateboard.Instance.gameObject.SetActive(Emoji == 0x0B);
        if (Emoji == 0x0B)
        {
            // speed & dash logic
            skateboardSpeed = Mathf.MoveTowards(skateboardSpeed, 20f, (slowsDown ? 24f : 12f) * Time.deltaTime);
            nm.boostCharge = Mathf.MoveTowards(nm.boostCharge, 300f, 70f * Time.deltaTime);

            if (InputManager.Instance.InputSource.Dodge.WasPerformedThisFrame)
            {
                if (nm.boostCharge >= 100f || (AssistController.Instance.majorEnabled && AssistController.Instance.infiniteStamina))
                {
                    skateboardSpeed += 20f;
                    nm.boostCharge -= 100f;

                    // major assists make it possible to dash endlessly so we need to clamp boost charge
                    if (nm.boostCharge < 0f) nm.boostCharge = 0f;

                    Instantiate(nm.dodgeParticle, nm.transform.position, nm.transform.rotation);
                    AudioSource.PlayClipAtPoint(nm.dodgeSound, nm.transform.position);
                }
                else Instantiate(nm.staminaFailSound);
            }

            if (skateboardSpeed >= 70f && !slowsDown)
            {
                slowsDown = true;
                fallParticle = Instantiate(nm.fallParticle, nm.transform);
            }
            if (skateboardSpeed <= 40f && slowsDown)
            {
                slowsDown = false;
                Destroy(fallParticle);
            }

            // move the skateboard forward
            var player = nm.transform;
            nm.rb.velocity = (player.forward * skateboardSpeed) with { y = nm.rb.velocity.y };

            // don’t let the front and rear wheels fall into the ground
            if (Physics.Raycast(player.position + player.forward * 1.2f, Vector3.down, out var hit, 1.5f, mask) && hit.distance > .8f)
                player.position = new(player.position.x, hit.point.y + 1.5f, player.position.z);

            if (Physics.Raycast(player.position - player.forward * 1.2f, Vector3.down, out var hit2, 1.5f, mask) && hit2.distance > .8f)
                player.position = new(player.position.x, hit2.point.y + 1.5f, player.position.z);

            // turn to the sides
            if (!UI.AnyMovementBlocking()) player.Rotate(new(0f, InputManager.Instance.InputSource.Move.ReadValue<Vector2>().x * 120f * Time.deltaTime, 0f));
        }

        // third person camera
        if (Emoji != 0xFF)
        {
            // cancel animation if space is pressed
            if (Input.GetKey(KeyCode.Space) && !Chat.Shown) StartEmoji(0xFF);

            // rotate the camera according to mouse sensitivity
            if (!UI.AnyJaket())
            {
                rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * OptionsManager.Instance.mouseSensitivity / 10f;
                rotation.y = Mathf.Clamp(rotation.y, 5f, 170f);
            }

            // turn on gravity, because if the taunt was launched on the ground, then it is disabled by default
            nm.rb.useGravity = true;

            var cam = cc.cam.transform;
            var player = nm.transform.position + new Vector3(0f, 1f, 0f);

            // move the camera position towards the start if the animation has just started, or towards the end if the animation ends
            bool ends = Time.time - EmojiStart > EmojiLegnth[Emoji] && EmojiLegnth[Emoji] != -1f;
            position = Vector3.MoveTowards(position, ends ? end : start, 12f * Time.deltaTime);

            // return the camera to its original position and rotate it around the player
            cam.position = player + position;
            cam.RotateAround(player, Vector3.left, rotation.y);
            cam.RotateAround(player, Vector3.up, rotation.x);
            cam.LookAt(player);

            // do not let the camera fall through the ground
            if (Physics.SphereCast(player, .25f, cam.position - player, out var hit, position.magnitude, mask))
                cam.position = hit.point + .5f * hit.normal;
        }

        // ultrasoap
        if (SceneHelper.CurrentScene != "Main Menu" && !nm.dead)
        {
            nm.rb.constraints = UI.AnyMovementBlocking()
                ? RigidbodyConstraints.FreezeAll
                : Instance.Emoji == 0xFF || Instance.Emoji == 0x0B // skateboard
                    ? RigidbodyConstraints.FreezeRotation
                    : (RigidbodyConstraints)122;
        }



        // all the following changes are related to the network part of the game and shouldn't affect the local
        if (LobbyController.Lobby == null) return;

        // pause stops time and weapon wheel slows it down, but in multiplayer everything should be real-time
        if (Settings.DisableFreezeFrames || UI.AnyBuiltIn()) Time.timeScale = 1f;

        // reset slam force if the player is riding on a rocket
        if (nm.ridingRocket != null) nm.slamForce = 0f;

        // disable cheats if they are prohibited in the lobby
        if (!LobbyController.CheatsAllowed && CheatsController.Instance.cheatsEnabled)
        {
            CheatsController.Instance.cheatsEnabled = false;
            CheatsManager.Instance.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);

            var cheats = AccessTools.DeclaredField(typeof(CheatsManager), "idToCheat").GetValue(CheatsManager.Instance) as Dictionary<string, ICheat>;
            cheats.Values.Do(CheatsManager.Instance.DisableCheat);

            UI.SendMsg("Cheats are prohibited in this lobby!");
        }

        // fake Cyber Grind death
        if (nm.dead && nm.endlessMode)
        {
            nm.blackScreen.gameObject.SetActive(true);
            nm.screenHud.SetActive(false);

            if (nm.blackScreen.color.a < 0.5f)
            {
                nm.blackScreen.color = nm.blackScreen.color with { a = nm.blackScreen.color.a + .75f * Time.deltaTime };
                nm.youDiedText.color = nm.youDiedText.color with { a = nm.blackScreen.color.a };
            }
        }
    }

    private void DeathScreenUpdate()
    {
        // disable text override component if the player is in tge Cyber Grind
        nm.youDiedText.GetComponents<MonoBehaviour>()[1].enabled = !nm.endlessMode;
        if (nm.endlessMode)
        {
            int alive = CyberGrind.PlayersAlive();
            nm.youDiedText.text = $"[YOUR UNIT IS DISABLED]\n\n\n\n\n\nWait until the next wave\nPlayers alive: [{alive}]";

            var final = nm.GetComponentInChildren<FinalCyberRank>();
            if (alive == 0 && final.savedTime == 0f)
            {
                final.GameOver();
                Destroy(nm.blackScreen.gameObject);
            }
        }
    }

    /// <summary> Returns the rounded speed of the skateboard. </summary>
    public int SkateboardSpeed() => (int)skateboardSpeed;

    /// <summary> Repeats a part of the checkpoint logic, needed in order to avoid resetting rooms. </summary>
    public void Respawn(Vector3 position, float rotation)
    {
        cc.activated = nm.enabled = true;
        nm.transform.position = position;
        nm.rb.velocity = Vector3.zero;

        if (PlayerTracker.Instance.playerType == PlayerType.FPS)
            cc.ResetCamera(rotation);
        else
            PlatformerMovement.Instance.ResetCamera(rotation);

        nm.Respawn();
        nm.GetHealth(0, true);
        cc.StopShake();
        nm.ActivatePlayer();
    }

    /// <summary> Respawns Cyber Grind players. </summary>
    public void CyberRespawn()
    {
        nm.Respawn();
        nm.transform.position = new(0f, 80f, 62.5f);
    }

    #region toggling

    /// <summary> Updates the state machine: toggles movement, cursor and third-person camera. </summary>
    public static void UpdateState()
    {
        ToggleMovement(!UI.AnyMovementBlocking() && Instance.Emoji == 0xFF);
        ToggleCursor(UI.AnyJaket());
        ToggleHud(Instance.Emoji == 0xFF);

        // block pause
        OptionsManager.Instance.frozen = Instance.Emoji != 0xFF || InteractiveGuide.Shown;

        // block camera rotation & weapon fire
        cc.enabled = cc.activated = GunControl.Instance.activated = !UI.AnyJaket() && Instance.Emoji == 0xFF;
    }

    /// <summary> Toggles the ability to move, used in the chat and etc. </summary>
    public static void ToggleMovement(bool enable)
    {
        nm.enabled = fc.enabled = fc.activated = HookArm.Instance.enabled = enable;

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
        fc.gameObject.SetActive(enable);

        // preventing some ultra stupid bug
        Console.Instance.enabled = enable;
    }

    #endregion
    #region emoji

    /// <summary> Creates a preview of the given emoji in player coordinates. </summary>
    public void PreviewEmoji(byte id)
    {
        EmojiPreview = Instantiate(DollAssets.Preview, nm.transform);
        EmojiPreview.transform.localPosition = new(0f, -1.5f, 0f);
        EmojiPreview.transform.localScale = new(2.18f, 2.18f, 2.18f); // preview created for terminal and too small

        var anim = EmojiPreview.transform.GetChild(0).GetComponent<Animator>();

        anim.SetTrigger("Show Emoji");
        anim.SetInteger("Emoji", id);
        anim.SetInteger("Rps", Rps);

        // apply team to emotion preview
        var team = Networking.LocalPlayer.Team;
        var mat1 = EmojiPreview.transform.GetChild(0).GetChild(4).GetComponent<Renderer>().materials[1];
        var mat2 = EmojiPreview.transform.GetChild(0).GetChild(3).GetComponent<Renderer>().materials[0];

        mat1.mainTexture = mat2.mainTexture = DollAssets.WingTextures[(int)team];
        if (team == Team.Pink) EmojiPreview.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

        if (id == 6) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(7).gameObject.SetActive(true); // throne
        if (id == 8) EmojiPreview.transform.GetChild(0).GetChild(1).GetChild(6).GetChild(10).GetChild(0).localEulerAngles = new(-20f, 0f, 0f); // neck
    }

    /// <summary> Triggers an emoji with the given id. </summary>
    public void StartEmoji(byte id, bool updateState = true)
    {
        EmojiStart = Time.time;
        Emoji = id; // save id for synchronization over the network

        // toggle movement and third-person camera
        if (updateState) UpdateState();

        // destroy the old preview so they don't stack
        Destroy(EmojiPreview);
        Destroy(fallParticle);

        // if id is -1, then the emotion was not selected
        if (id == 0xFF) return;
        else PreviewEmoji(id);

        // telling how to interrupt an emotion
        UI.SendMsg("Press <color=orange>Space</color> to interrupt the emotion", true);

        // stop sliding so that the preview is not underground
        nm.playerCollider.height = 3.5f;
        nm.gc.transform.localPosition = new(0f, -1.256f, 0f);

        // rotate the third person camera in the same direction as the first person camera
        rotation = new(cc.rotationY, cc.rotationX + 90f);
        position = new();
        skateboardSpeed = 0f;

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
