namespace Jaket.Input;

using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.World;

/// <summary> Class responsible for playing emotes. </summary>
public class Emotes : MonoSingleton<Emotes>
{
    static NewMovement nm => NewMovement.Instance;

    /// <summary> Array containing the length of all emotes in seconds. </summary>
    public static readonly float[] Length = { 2.458f, 4.708f, 1.833f, 2.875f, 0f, 9.083f, -1f, 11.022f, -1f, 3.292f, 0f, -1f };
    /// <summary> Identifier of the currently playing emote. </summary>
    public static byte Current = 0xFF, Rps;

    /// <summary> Doll that plays the current emote. </summary>
    public GameObject Preview;
    /// <summary> Time of the current emote start. </summary>
    public float StartTime;
    /// <summary> Whether the current emote is about to end. </summary>
    public static bool Ends => Time.time - Instance.StartTime > Length[Current] && Length[Current] != -1f;

    private void Start()
    {
        // interrupt emote to prevent some bugs
        Events.OnLoad += () => Instance.Play(0xFF);
    }

    private void Update()
    {
        // interrupt emote if space is pressed
        if (Current != 0xFF && Input.GetKey(KeyCode.Space)) Play(0xFF);
    }

    /// <summary> Plays the given emote animation. </summary>
    public void Play(byte emote)
    {
        Current = emote;
        StartTime = Time.time;

        Movement.UpdateState();

        Dest(Preview);
        Dest(Movement.Instance.FallParticle);

        if (emote == 0xFF) return;

        Preview = Doll.Spawn(nm.transform, Networking.LocalPlayer.Team, Shop.SelectedHat, Shop.SelectedJacket, emote, Rps).gameObject;
        Bundle.Hud("emote", true);

        // stop sliding to prevent preview from falling underground
        nm.playerCollider.height = 3.5f;
        nm.gc.transform.localPosition = new(0f, -1.256f, 0f);

        // rotate the third person camera in the same direction as the first person camera
        UI.Spectator.Reset();
        Movement.Instance.SkateboardSpeed = 0f;

        // restart the coroutine if the emote is not infinite
        StopCoroutine("ClearEmote");
        if (Length[emote] != -1f) StartCoroutine("ClearEmote");
    }

    /// <summary> Clears the current emote after the end of its animation. </summary>
    public IEnumerator Clear()
    {
        yield return new WaitForSeconds(Length[Current] + .5f);

        if (Current == 0x03) LobbyController.Lobby?.SendChatString("#/r" + Rps);
        Play(0xFF);
    }
}
