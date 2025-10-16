namespace Jaket.Input;

using GameConsole;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.UI.Dialogs;
using Jaket.UI.Elements;
using Jaket.UI.Fragments;

/// <summary> Class responsible for additions to control and local display of emotes. </summary>
public class Movement : MonoSingleton<Movement>
{
    static NewMovement nm => NewMovement.Instance;
    static FistControl fc => FistControl.Instance;
    static GunControl gc => GunControl.Instance;
    static CameraController cc => CameraController.Instance;
    static CheatsController ch => CheatsController.Instance;
    static CheatsManager cm => CheatsManager.Instance;

    /// <summary> Last point created by the player. </summary>
    private Point point;
    /// <summary> Last spray created by the player. </summary>
    private Spray spray;
    /// <summary> Hold time of the emote wheel key. </summary>
    private float holdTime;

    private void Start()
    {
        Events.OnLoad += () =>
        {
            if (Scene == "Level 0-S") nm.modNoJump = LobbyController.Online;
            if (Scene == "Level 0-S" || Scene == "Endless")
            {
                CanvasController.Instance.transform.Find("PauseMenu/Restart Mission").GetComponent<Button>().interactable = LobbyController.Offline || LobbyController.IsOwner;
            }
            UpdateState(true);
        };
        Events.EveryHalf += () =>
        {
            if (ch.cheatsEnabled && LobbyController.Online && !Administration.Privileged.Contains(AccId))
            {
                ch.cheatsEnabled = false;
                cm.transform.Find("Cheats Overlay").Each(c => c.gameObject.SetActive(false));

                (Get("idToCheat", cm) as Dictionary<string, ICheat>).Values.Each(cm.DisableCheat);
                Bundle.Hud("unprivileged");
            }
        };
    }

    private void Update()
    {
        if (Scene == "Main Menu") return;

        if (Keybind.ScrollUp.Tap()) UI.Chat.Scroll(true);
        if (Keybind.ScrollDown.Tap()) UI.Chat.Scroll(false);

        if (UI.Focused || UI.Settings.Rebinding != null) return;

        if (Keybind.LobbyTab.Tap())   UI.LobbyTab.Toggle();
        if (Keybind.PlayerList.Tap()) UI.PlayerList.Toggle();
        if (Keybind.Settings.Tap())   UI.Settings.Toggle();
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

            // turn on gravity, because if the taunt was launched on the ground, then it is disabled by default
            nm.rb.useGravity = true;
        }

        if (!nm.dead) nm.rb.constraints = UI.AnyDialog
            ? RigidbodyConstraints.FreezeAll
            : Emotes.Current == 0xFF || Emotes.Current == 0x0B
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
    }

    private void OnGUI()
    {
        if (UI.Settings.Rebinding != null) UI.Settings.RebindUpdate();
    }

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
        Teleporter.Tp(position, flash);

        nm.Respawn();
        nm.GetHealth(0, true);
        nm.ActivatePlayer();

        cc.ResetCamera(rotation);
        cc.StopShake();

        /* TODO move checkpoints instead of doing this
        // the player is currently fighting the Minotaur in the tunnel, the security system or the brain in the Earthmover
        if (World.TunnelRoomba) nm.transform.position = World.TunnelRoomba.position with { y = -112.5f };
        if (World.SecuritySystem[0]) nm.transform.position = new(0f, 472f, 745f);
        if (World.Brain && World.Brain.IsFightActive) nm.transform.position = new(0f, 826.5f, 610f);
        */
    }

    /// <summary> Respawns Cyber Grind players and flashes the screen. </summary>
    public static void CyberRespawn() => Respawn(new(0f, 80f, 62.5f), 0f, true);

    /// <summary> Kills the player immediately. </summary>
    public static void Suicide() => nm.GetHurt(nm.hp, true, 0f, instablack: true, ignoreInvincibility: true);

    #endregion
    #region harmony

    [HarmonyPatch(typeof(OptionsManager), "LateUpdate")]
    [HarmonyPostfix]
    static void Scale()
    {
        if (LobbyController.Online && Settings.DisableFreezeFrames | UI.AnyDialog) Time.timeScale = 1f;
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    [HarmonyPrefix]
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

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    [HarmonyPrefix]
    static void Invincibility(ref float ___hurtInvincibility)
    {
        if (___hurtInvincibility > .08f) ___hurtInvincibility = .08f;
    }

    [HarmonyPatch(typeof(CheatsManager), nameof(CheatsManager.HandleCheatBind))]
    [HarmonyPrefix]
    static bool CheatBind() => !(UI.AnyDialog || Emotes.Current != 0xFF || nm.dead);

    [HarmonyPatch(typeof(CheatsController), nameof(CheatsController.Update))]
    [HarmonyPrefix]
    static bool CheatMenu() => CheatBind();

    [HarmonyPatch(typeof(ULTRAKILL.Cheats.Noclip), "UpdateTick")]
    [HarmonyPrefix]
    static bool CheatNoclip() => CheatBind();

    [HarmonyPatch(typeof(ULTRAKILL.Cheats.Flight), "Update")]
    [HarmonyPrefix]
    static bool CheatFlight() => CheatBind();

    [HarmonyPatch(typeof(Grenade), "Update")]
    [HarmonyPrefix]
    static bool GrenadeRide() => CheatBind();

    [HarmonyPatch(typeof(WeaponWheel), "OnEnable")]
    [HarmonyPrefix]
    static void Superiority()
    {
        if (UI.Emote.Shown) WeaponWheel.Instance.gameObject.SetActive(false);
    }

    [HarmonyPatch(typeof(CameraFrustumTargeter), "CurrentTarget", MethodType.Setter)]
    [HarmonyPrefix]
    static void LoosersLove(ref Collider value)
    {
        if (value && value.TryGetComponent(out Entity.Agent a) && a.Patron is RemotePlayer p && p.Team.Ally()) value = null;
    }

    #endregion
}
