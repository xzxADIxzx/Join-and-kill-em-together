namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

/// <summary> In fact, it's just a flash needed to make the teleportation look better. </summary>
public class Teleporter : CanvasSingleton<Teleporter>
{
    /// <summary> Flash of light covering the entire screen. </summary>
    private Image flash, decor;
    /// <summary> Sound that plays after teleportation. </summary>
    private AudioClip click;

    private void Start()
    {
        flash = UIB.Image("Flash", transform, new(0f, 0f, 0f, 0f, Vector2.zero, Vector2.one), Color.white with { a = 0f });
        decor = UIB.Image("Decoration", transform, new(2000f, 0f, 64f, 2000f), Color.white);
        decor.sprite = null;
        decor.transform.localEulerAngles = new(0f, 0f, -25f);

        Tools.ResFind<AudioClip>(clip => clip.name == "Click1", clip => click = clip);
    }

    private void Update()
    {
        flash.color = Color.white with { a = Mathf.MoveTowards(flash.color.a, 0f, Time.deltaTime) };
        decor.transform.localPosition = new(Mathf.MoveTowards(decor.transform.localPosition.x, 2000f, Time.deltaTime * 8000f), 0f);

        if (flash.color.a <= 0.0001f) gameObject.SetActive(false);
    }

    /// <summary> Launches a flash of light. </summary>
    public void Flash()
    {
        flash.color = Color.white;
        decor.transform.localPosition = new(-2000f, 0f);

        gameObject.SetActive(true);
    }

    /// <summary> Teleports the player to the given coordinates. </summary>
    public void Teleport(Vector3 pos)
    {
        NewMovement.Instance.transform.position = pos;
        Events.Post2(() => AudioSource.PlayClipAtPoint(click, pos));

        Flash();
    }
}
