namespace Jaket.UI.Fragments;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Content;
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

    /// <summary> Whether the current scene has a special behavior. </summary>
    public static bool Special => Scene == "Endless" || Scene == "Level 0-S";

    /// <summary> Text imitating machine's terminal. </summary>
    private Text info;
    /// <summary> Image displaying a mysterious eye. </summary>
    private Image dead;

    /// <summary> Third person camera position. </summary>
    private Vector3 position;
    /// <summary> Third person camera rotation. </summary>
    private Vector2 rotation;
    /// <summary> Third person camera target. If an emote is playing, the camera aims at the local player, otherwise, at the remote player. </summary>
    private int targetPlayer;

    public Spectator(Transform root) : base(root, "Spectator", false)
    {
        Events.EveryHalf += () => Shader.SetGlobalFloat
        (
            "_Deathness",
            Random.value / (3f + Random.value)
        );
        Events.EveryHalf += () => Shader.SetGlobalFloat("_RandomNoiseStrength", .1f);

        info = Builder.Text(Fill("Info"), "hi", 24, white, TextAnchor.UpperLeft);
        dead = Builder.Image(Fill("Eye"), Tex.Dead, white, ImageType.Simple);

        info.rectTransform.sizeDelta = Vector2.one * -128f;

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
        Content.gameObject.SetActive(Shown);
        PostProcessV2_Handler.Instance.DeathEffect(Shown);
        PostProcessV2_Handler.Instance.WickedEffect(Shown);
    }

    public override void Rebuild() => info.text = Bundle.Format
    (
        "spect",
        Keybind.SpectNext.FormatValue(),
        Keybind.SpectPrev.FormatValue(),
        Special || Gameflow.LockRespawn ? "#spect.perm" : "#spect.temp",
        Special
        ? (
            Scene == "Endless" ? "#spect.waves" : "#spect.death"
        ) :
        Gameflow.LockRespawn
        ? (
            Gameflow.Mode == Gamemode.Hardcore ? "#spect.death" : "#spect.round"
        ) :
        "#spect.press"
    );

    #region camera

    public void UpdateCamera(bool ends)
    {
        var camera = cc.cam.transform;
        var player = nm.dead && Networking.Entities.TryGetValue(LobbyController.MemberId(targetPlayer), out var e) && e is RemotePlayer rp
            ? rp.Position
            : nm.transform.position + Vector3.up;

        position = Vector3.MoveTowards(position, Vector3.up * (ends ? .1f : 6f), Time.deltaTime * 12f);

        camera.position = player + position;
        camera.RotateAround(player, Vector3.left, rotation.y);
        camera.RotateAround(player, Vector3.up, rotation.x);
        camera.LookAt(player);

        if (Physics.SphereCast(player, .2f, camera.position - player, out var hit, position.magnitude, EnvMask)) camera.position = hit.point + hit.normal;
    }

    public void UpdateInput()
    {
        if (UI.AnyDialog) return;
        if (Input.GetKeyDown(KeyCode.R) && !Special && !Gameflow.LockRespawn)
        {
            Shown = false;
            Toggle();
            Events.Post(StatsManager.Instance.Restart);
        }

        rotation += InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * 2f;
        rotation.y = Mathf.Clamp(rotation.y, 1f, 179f);

        if (Keybind.SpectNext.Tap()) targetPlayer++;
        if (Keybind.SpectPrev.Tap()) targetPlayer--;

        if (targetPlayer < 0) targetPlayer = LobbyController.Lobby?.MemberCount - 1 ?? 0;
        if (targetPlayer >= LobbyController.Lobby?.MemberCount) targetPlayer = 0;
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
    static bool Restart() => !nm.deathSequence.gameObject.activeSelf && !UI.Spectator.Shown;

    [HarmonyPatch(typeof(DeathSequence), nameof(DeathSequence.EndSequence))]
    [HarmonyPostfix]
    static void Sequence(DeathSequence __instance) => Events.Post(() =>
    {
        __instance.gameObject.SetActive(false);

        UI.Spectator.Shown = true;
        UI.Spectator.Toggle();
        UI.Spectator.Rebuild();
        UI.Spectator.Reset();
    });

    #endregion
}
