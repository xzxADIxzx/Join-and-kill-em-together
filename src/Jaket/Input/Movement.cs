namespace Jaket.Input;

using GameConsole;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Harmony;
using Jaket.Net;
using Jaket.Net.Admin;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.UI.Elements;
using Jaket.UI.Fragments;
using Jaket.World;

/// <summary> Class responsible for additions to control and local display of emotes. </summary>
public class Movement : MonoSingleton<Movement>
{
    static NewMovement nm => NewMovement.Instance;
    static FistControl fc => FistControl.Instance;
    static GunControl gc => GunControl.Instance;
    static CameraController cc => CameraController.Instance;
    static AssistController ac => AssistController.Instance;
    static CheatsController ch => CheatsController.Instance;
    static CheatsManager cm => CheatsManager.Instance;

    /// <summary> Last point created by the player. </summary>
    private Point point;
    /// <summary> Last spray created by the player. </summary>
    private Spray spray;
    /// <summary> Hold time of the emote wheel key. </summary>
    private float holdTime;

    #region general

    private void Start()
    {
        Events.OnLoad += () => UpdateState(true);
        Events.EveryHalf += () =>
        {
            if (ch.cheatsEnabled && LobbyController.Online && !Administration.Privileged)
            {
                cm.transform.Find("Cheats Overlay").Each(c => c.gameObject.SetActive(false));
                cm.idToCheat.Values.Each(cm.DisableCheat);

                ch.cheatsEnabled = false;
                Bundle.Hud("unprivileged");
            }
            if (ac.majorEnabled && LobbyController.Online)
            {
                ac.majorEnabled = false;
                Bundle.Hud("major-assist");
            }
        };
    }

    private void Update()
    {
        if (Scene == "Main Menu") return;

        if (Keybind.ScrollUp.Tap()) UI.Chat.Scroll(true);
        if (Keybind.ScrollDown.Tap()) UI.Chat.Scroll(false);

        if (UI.Focused || UI.Settings.Rebinding != null) return;

        if (Keybind.LobbyTab  .Tap()) UI.LobbyTab  .Toggle();
        if (Keybind.PlayerList.Tap()) UI.PlayerList.Toggle();
        if (Keybind.Settings  .Tap()) UI.Settings  .Toggle();
        if (Keybind.PlayerInds.Tap()) UI.PlayerInds.Toggle();
        if (Keybind.PlayerInfo.Tap()) UI.PlayerInfo.Toggle();

        if ((Keybind.Point.Tap() || Keybind.Spray.Tap()) && Physics.Raycast(cc.transform.position, cc.transform.forward, out var hit, float.MaxValue, EnvMask))
        {
            // rotate the normal towards the local player a little so that when placed on the ground it is rotated correctly
            var normal = Vector3.RotateTowards(hit.normal, nm.transform.position - hit.point, .001f, 0f);

            if (Keybind.Point.Tap())
            {
                if (point) point.Lifetime = 5.5f;
                point = Point.Spawn(hit.point, normal, Networking.LocalPlayer.Team);
            }
            if (Keybind.Spray.Tap())
            {
                if (spray) spray.Lifetime = 58f;
                spray = Spray.Spawn(hit.point, normal, Networking.LocalPlayer.Team);
            }
            if (LobbyController.Online) Networking.Send(Keybind.Point.Tap() ? PacketType.Point : PacketType.Spray, 28, w =>
            {
                w.Id(AccId);
                w.Vector(hit.point);
                w.Vector(normal);
            });
        }

        if (Keybind.EmoteWheel.Down() && !WeaponWheel.Instance.gameObject.activeSelf)
        {
            holdTime += Time.deltaTime;
            if (!UI.Emote.Shown && holdTime > .25f) UI.Emote.Show();
        }
        else
        {
            holdTime = 0f;
            if (UI.Emote.Shown) UI.Emote.Hide();
        }

        if (Keybind.Chat.Tap()) UI.Chat.Toggle();
        if (Keybind.Spectate.Tap()) Suicide();

        if (Input.GetKeyDown(KeyCode.F4)) UI.Debug.Toggle();
        if (Input.GetKeyDown(KeyCode.F5)) UI.Debug.Clear();
        if (Input.GetKeyDown(KeyCode.F6)) UI.Debug.Raycast();
    }

    private void LateUpdate()
    {
        if (Scene == "Main Menu") return;

        if (UI.Skateboard.Shown && !UI.AnyDialog)
        {
            UI.Skateboard.UpdateInput();
            UI.Skateboard.UpdateVehicle();
        }

        if (Emotes.Current != 0xFF || (LobbyController.Online && nm.dead))
        {
            UI.Spectator.UpdateInput();
            UI.Spectator.UpdateCamera(!nm.dead && Emotes.Ends);
        }

        if (!nm.dead) nm.rb.constraints = UI.AnyDialog
            ? RigidbodyConstraints.FreezeAll
            : Emotes.Current == 0xFF || Emotes.Current == 0x0B
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
    }

    private void OnGUI()
    {
        if (UI.Settings?.Rebinding != null) UI.Settings.RebindUpdate();
    }

    #endregion
    #region control

    /// <summary> Updates the state machine: toggles movement, cursor, hud and weapons. </summary>
    public static void UpdateState(bool falling = false)
    {
        static void ToggleCursor(bool enable)
        {
            Cursor.visible = enable;
            Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        }
        static void ToggleHud(bool enable)
        {
            nm.screenHud.SetActive(enable);
            gc.gameObject.SetActive(enable);
        }
        bool
            dialog = UI.AnyDialog,
            locked = dialog || Emotes.Current != 0xFF || (LobbyController.Offline && falling);

        ToggleCursor(dialog || Scene == "Level 2-S");
        ToggleHud(Emotes.Current == 0xFF && !nm.dead);

        if (nm.dead) return;

        nm.activated = fc.activated = gc.activated = !locked;
        cc.activated = !locked && !UI.Emote.Shown;

        if (!locked) fc.YesFist();

        OptionsManager.Instance.frozen = Emotes.Current != 0xFF;
        Console.Instance.enabled       = Emotes.Current == 0xFF;
    }

    /// <summary> Respawns the player at the given position with the given rotation. </summary>
    public static void Respawn(Vector3 position, float rotation, bool flash = false)
    {
        StyleHUD.Instance.ComboOver();
        StyleHUD.Instance.ResetAllFreshness();

        nm.Respawn();
        nm.GetHealth(0, true);
        nm.ActivatePlayer();

        cc.ResetCamera(rotation);
        cc.StopShake();
        fc.fistCooldown = 0f;

        Teleporter.Tp(position, flash);
    }

    /// <summary> Respawns Cyber Grind players and flashes the screen. </summary>
    public static void CyberRespawn() => Respawn(new(0f, 80f, 62.5f), 0f, true);

    /// <summary> Kills the player immediately. </summary>
    public static void Suicide() => nm.GetHurt(nm.hp, true, 0f, instablack: true, ignoreInvincibility: true);

    #endregion
    #region harmony

    [StaticPatch(typeof(OptionsManager), nameof(OptionsManager.Pause))]
    [Prefix]
    static void Fixes(ref GunControl ___gc) => ___gc = GunControl.Instance; // idk why, but it keeps happening randomly

    [StaticPatch(typeof(OptionsManager), nameof(OptionsManager.UnPause))]
    [Postfix]
    static void Pause() => UpdateState();

    [DynamicPatch(typeof(OptionsManager), nameof(OptionsManager.LateUpdate))]
    [Postfix]
    static void Scale() => Time.timeScale = Gameflow.Slowmo ? .5f : 1f;

    [StaticPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    [Prefix]
    static void Death(NewMovement __instance, int damage, bool invincible, bool ignoreInvincibility)
    {
        if (invincible && __instance.gameObject.layer == 15 && !ignoreInvincibility) return;
        if (ULTRAKILL.Cheats.Invincibility.Enabled) return;

        if (__instance.hp > 0 && __instance.hp - damage <= 0)
        {
            LobbyController.Lobby?.SendChatString("#/d");
            Emotes.Instance.Play(0xFF);
        }
    }

    [DynamicPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    [Postfix]
    static void Invincibility(ref float ___hurtInvincibility)
    {
        if (___hurtInvincibility > .08f) ___hurtInvincibility = .08f;
    }

    [StaticPatch(typeof(CheatsManager),           nameof(CheatsManager.HandleCheatBind))]
    [StaticPatch(typeof(CheatsController),        nameof(CheatsController.Update))]
    [StaticPatch(typeof(ULTRAKILL.Cheats.Noclip), nameof(ULTRAKILL.Cheats.Noclip.UpdateTick))]
    [StaticPatch(typeof(ULTRAKILL.Cheats.Flight), nameof(ULTRAKILL.Cheats.Flight.Update))]
    [StaticPatch(typeof(Grenade),                 nameof(Grenade.Update))]
    [Prefix]
    static bool GrenadeRide() => !(UI.AnyDialog || Emotes.Current != 0xFF || nm.dead);

    [StaticPatch(typeof(WeaponWheel), nameof(WeaponWheel.OnEnable))]
    [Prefix]
    static void Superiority()
    {
        if (UI.Emote.Shown) WeaponWheel.Instance.gameObject.SetActive(false);
    }

    [StaticPatch(typeof(CameraFrustumTargeter), nameof(CameraFrustumTargeter.CurrentTarget), HarmonyLib.MethodType.Setter)]
    [Prefix]
    static void LoosersLove(ref Collider value)
    {
        if (value && value.TryGetEntity(out RemotePlayer p) && p.Team.Ally()) value = null;
    }

    #endregion
}
