namespace Jaket.UI.Fragments;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Input;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI.Lib;
using Jaket.World;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that is displayed when the player is dead. </summary>
public class Spectator : Fragment
{
    static NewMovement nm => NewMovement.Instance;
    static CameraController cc => CameraController.Instance;

    /// <summary> Whether the current scene has a special info text. </summary>
    public static bool Special => Scene == "Endless" || Scene == "Level 0-S";

    /// <summary> Information about keybindings that are usable at the moment. </summary>
    private Text info;
    /// <summary> Flash that displays the I See You texture. </summary>
    private Image dead;

    /// <summary> Third person camera position. </summary>
    private Vector3 position;
    /// <summary> Third person camera rotation. </summary>
    private Vector2 rotation;
    /// <summary> Third person camera target. If an emote is playing, the camera aims at the local player, otherwise, at the remote player. </summary>
    private int targetPlayer;

    public Spectator(Transform root) : base(root, "Spectator", false)
    {
        Events.EveryHalf += () =>
        {
            if (LobbyController.Online && Special) UpdateAlive();
        };

        info = Builder.Text(Fill("Info"), "", 96, white with { a = semi.a }, TextAnchor.MiddleCenter);
        dead = Builder.Image(Fill("Flash"), Tex.Dead, white, ImageType.Simple);

        Component<HudOpenEffect>(dead.gameObject, e =>
        {
            e.reverse = true;
            e.YFirst = true;
        });
        GameAssets.Sound("UI/TV Off.wav", c =>
        {
            Component<AudioSource>(info.gameObject, a => a.clip = c);
            Component<AudioSource>(dead.gameObject, a => a.clip = c);
        });
    }

    public override void Toggle()
    {
        Content.gameObject.SetActive(Shown = NewMovement.Instance.dead);

        if (Shown) info.text = Bundle.Format("spect",
            Keybind.SpectNext.FormatValue(),
            Keybind.SpectPrev.FormatValue(),
            Special ? Bundle.Format("spect.special", Scene == "Endless" ? "#spect.cg" : "#spect.zs") : "#spect.default");
    }

    #region camera

    public void UpdateCamera(bool ends)
    {
        var camera = cc.cam.transform;
        var player = nm.dead && Networking.Entities.TryGetValue(LobbyController.MemberId(targetPlayer), out var e) && e is RemotePlayer rp
            ? rp.Position
            : nm.transform.position + Vector3.up;

        position = Vector3.MoveTowards(position, Vector3.up * (ends ? .1f : 6f), 12f * Time.deltaTime);

        camera.position = player + position;
        camera.RotateAround(player, Vector3.left, rotation.y);
        camera.RotateAround(player, Vector3.up, rotation.x);
        camera.LookAt(player);

        if (Physics.SphereCast(player, .2f, camera.position - player, out var hit, position.magnitude, EnvMask)) camera.position = hit.point + hit.normal;
    }

    public void UpdateInput()
    {
        if (UI.AnyDialog) return;
        if (Input.GetKeyDown(KeyCode.R) && !Special)
        {
            Content.gameObject.SetActive(Shown = false);
            StatsManager.Instance.Restart();
        }

        rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * 2f;
        rotation.y = Mathf.Clamp(rotation.y, 0f, 180f);

        if (Keybind.SpectNext.Tap()) targetPlayer++;
        if (Keybind.SpectPrev.Tap()) targetPlayer--;

        if (targetPlayer < 0) targetPlayer = LobbyController.Lobby?.MemberCount - 1 ?? 0;
        if (targetPlayer >= LobbyController.Lobby?.MemberCount) targetPlayer = 0;
    }

    public void UpdateAlive()
    {
        if (CyberGrind.PlayersAlive() > 0) return;
        Content.gameObject.SetActive(Shown = false);

        if (Scene == "Endless")
        {
            var rank = nm.GetComponentInChildren<FinalCyberRank>();
            if (rank.savedTime == 0f) rank.GameOver();
        }
        else if (LobbyController.IsOwner) StatsManager.Instance.Restart();
    }

    public void Reset()
    {
        position = new();
        rotation = new(cc.rotationY, cc.rotationX + 90f);
        targetPlayer = LobbyController.Lobby?.Members.IndexOf(m => m.IsMe) ?? 0;
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Restart))]
    [HarmonyPrefix]
    static bool Restart() => !UI.Spectator.Shown && !Dialogs.Chat.Shown;

    [HarmonyPatch(typeof(DeathSequence), nameof(DeathSequence.EndSequence))]
    [HarmonyPostfix]
    static void Sequence(DeathSequence __instance) => Events.Post(() =>
    {
        __instance.gameObject.SetActive(false);

        UI.Spectator.Toggle();
        UI.Spectator.Reset();
    });

    #endregion
}
