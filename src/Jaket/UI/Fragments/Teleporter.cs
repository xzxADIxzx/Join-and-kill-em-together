namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.UI.Lib;
using Jaket.World;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that is responsible for teleportation and the Very Bright Flash™. </summary>
public class Teleporter : Fragment
{
    static NewMovement nm => NewMovement.Instance;

    /// <summary> Flash of light covering the entire screen. </summary>
    private Image flash, decor;
    /// <summary> Sound that plays after teleportation. </summary>
    private AudioClip clip;

    public Teleporter(Transform root) : base(root, "Teleporter", false)
    {
        flash = Builder.Image(Fill("Flash"),                  null, white, ImageType.Simple);
        decor = Builder.Image(Rect("Decor", new(64f, 2048f)), null, white, ImageType.Simple);
        decor.transform.localEulerAngles = new(0f, 0f, -24f);

        Component<Bar>(Content.gameObject, b => b.Update(() =>
        {
            flash.color = white with { a = Mathf.MoveTowards(flash.color.a, 0f, Time.deltaTime) };
            decor.transform.localPosition = new(Mathf.MoveTowards(decor.transform.localPosition.x, 2048f, 8192f * Time.deltaTime), 0f);

            if (flash.color.a == 0f) Content.gameObject.SetActive(Shown = false);
        }));

        GameAssets.Sound("UI/dark-ui power on off dark 02.wav", c => clip = c);
    }

    public override void Toggle()
    {
        Content.gameObject.SetActive(Shown = true);
        flash.color = white;
        decor.transform.localPosition = new(-2048f, 0f);
    }

    /// <summary> Teleports the player to the given position and activates movement. </summary>
    public static void Tp(Vector3 position, bool flash = true, bool insideEarthmover = true)
    {
        Movement.UpdateState();
        nm.transform.position = position;
        nm.rb.velocity = Vector3.zero;

        PlayerActivatorRelay.Instance?.Activate();
        if (GameStateManager.Instance.IsStateActive("pit-falling"))
            GameStateManager.Instance.PopState("pit-falling");

        if (flash) UI.Teleporter.Toggle();

        // this annoying sound makes me cry
        ObjFind("Hellmap")?.SetActive(false);

        // but this one is fine, I like it
        Events.Post2(() => AudioSource.PlayClipAtPoint(UI.Teleporter.clip, position));

        // load necessary locations to prevent players from getting out of bounds
        if (Scene == "Level 7-4") ObjFind(insideEarthmover ? "InsideActivator" : "OutsideActivator").GetComponent<ObjectActivator>().Activate();
    }
}
