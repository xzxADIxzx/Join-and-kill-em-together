namespace Jaket.UI;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

using Jaket.World;

/// <summary> Cinema interface used to start and control video. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class CinemaPlayer : MonoSingleton<CinemaPlayer>
{
    /// <summary> Cached cinema video player component. </summary>
    public VideoPlayer Player;
/*
    /// <summary> Viewing time and video length. </summary>
    private Text time;
    /// <summary> Background of the time. </summary>
    private RectTransform timeBg;

    /// <summary> Creates a singleton of cinema player. </summary>
    public static void Build()
    {
        // initialize the singleton and create a canvas
        Utils.Canvas("Cinema Player", Plugin.Instance.transform).AddComponent<CinemaPlayer>();

        // hide cinema player once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => Instance.gameObject.SetActive(false);
    }

    private void Start()
    {
        // TODO replace with Utils.WorldCanvas
        GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        // build cinema player
        timeBg = Utils.Image("", transform, 0f, 0f, 0f, 0f).transform as RectTransform;
        time = Utils.Text("", timeBg, 0f, 0f, 1000f, 64f, 32).GetComponent<Text>();

        // for some unknown reason, the canvas needs to be scaled and moved after adding elements
        canvas.transform.position = new Vector3(289.3f, 78.3f, 713.5f);
        canvas.transform.localScale = new Vector3(.022f, .022f, .022f);

        // start a loop that will update the time and the timeline slider
        InvokeRepeating("UpdateTime", 0f, 1f);
    }

    /// <summary> Updates the time and timeline slider. </summary>
    public void UpdateTime()
    {
        // the player is not in the cinema or has not yet started the video
        if (Player == null) return;

        // format the text just like on YouTube
        time.text = $"{(int)(Player.time / 60)}:{(int)(Player.time % 60)} / {(int)(Player.length / 60)}:{(int)(Player.length % 60)}";
        float width = time.text.Length * 18f + 32f;

        timeBg.sizeDelta = new Vector2(width, 64f); // cinema canvas has a slightly different size
        timeBg.anchoredPosition = new Vector2(-952f + 16f + width / 2f, -588f + 16f + 32f);
    }
*/
    /// <summary> Shows the video player and updates the cache. </summary>
    public void Play()
    {
        Player = Cinema.Player();
        gameObject.SetActive(true);
    }
}
