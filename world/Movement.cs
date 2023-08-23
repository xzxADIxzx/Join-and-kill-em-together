namespace Jaket.World;

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
        // all the following changes are related to the network part of the game and shouldn't affect the local
        if (LobbyController.Lobby == null) return;

        // pause stops time and weapon wheel slows it down, but in multiplayer everything should be real-time
        if (OptionsManager.Instance.paused || WeaponWheel.Instance.gameObject.activeSelf) Time.timeScale = 1f;

        // sometimes it happens that in the chat the player flies into the air
        if (!NewMovement.Instance.dead) rb.constraints = NewMovement.Instance.enabled ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
    }

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
}
