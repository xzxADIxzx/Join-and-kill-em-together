namespace Jaket.UI.Lib;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Content;
using Jaket.Net;

using static Pal;

/// <summary> Either horizontal or vertical bar that gets filled with interface elements. </summary>
public class Bar : MonoBehaviour
{
    /// <summary> Whether the bar is vertical or horizontal. </summary>
    private bool voh;
    /// <summary> Margin from the borders and padding between the elements. </summary>
    private float margin, padding;
    /// <summary> Amount of pixels claimed by the elements. </summary>
    private float filled;
    /// <summary> Action to be done in the update loop. </summary>
    private Runnable update;
    /// <summary> Rectangle that contains this element. </summary>
    private RectTransform rect;

    /// <summary> Whether the bar is empty or has children. </summary>
    public bool Empty => filled == 0f;

    /// <summary> Sets up the basic options of the bar. </summary>
    public void Setup(bool voh, float margin = 8f, float padding = 8f)
    {
        TryGetComponent(out rect);
        this.voh = voh;
        this.margin = margin;
        this.padding = padding;
    }

    /// <summary> Clears the bar by destroying children of it. </summary>
    public void Clear()
    {
        rect.Each(Dest);
        filled = 0f;
    }

    private void Update() => update?.Invoke();

    /// <summary> Schedules the given runnable to be done in the update loop. </summary>
    public void Update(Runnable update)
    {
        var previous = this.update;
        this.update = () =>
        {
            previous?.Invoke();
            update();
        };
    }

    #region base

    /// <summary> Resolves the given size of an element and returns a rect to build the element in. </summary>
    public RectTransform Resolve(string name, float size)
    {
        float fill = (voh ? rect.sizeDelta.x : rect.sizeDelta.y) - margin * 2f;
        float incr = (filled != 0f ? padding : margin) + size / 2f;

        var result = Builder.Rect(name, rect, new(
            voh ? 0f : filled += incr,
            voh ? filled -= incr : 0f,
            voh ? fill : size,
            voh ? size : fill,
            voh ? new(.5f, 1f) : new(0f, .5f)
        ));

        filled += size / (voh ? -2f : 2f);
        return result;
    }

    /// <summary> Adds a subbar, does not configure it. </summary>
    public void Subbar(float size, Cons<Bar> cons) =>
        Component(Resolve("Subbar", size).gameObject, cons);

    /// <summary> Adds a label with the given text, simple yet elegant. </summary>
    public Text Text(string text, float spc, int size = 24, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter) =>
        Builder.Text(Resolve("Text", spc), text, size, color ?? white, align);

    /// <summary> Adds a pair of labels with different alignment, horizontal only. </summary>
    public void Text(string text, float spc, out Text display, int size = 24, Color? color = null)
    {
        var txt = Builder.Text(Resolve("Pair", spc),                  text, size, color ?? white, TextAnchor.MiddleLeft).transform;
        display = Builder.Text(Builder.Rect("Display", txt, Rect.Fill), "", size, color ?? white, TextAnchor.MiddleRight);
    }

    #endregion
    #region image

    /// <summary> Adds an image with the given sprite, rarely used. </summary>
    public Image Image(Sprite sprite, float spc, Color? color = null, ImageType type = ImageType.Sliced, float? multiplier = null) =>
        Builder.Image(Resolve("Image", spc), sprite, color ?? white, type, multiplier);

    /// <summary> Adds an image separator, rarely used. </summary>
    public Image Separator() =>
        Builder.Image(Resolve("Separator", 16f), Tex.Dash, red, ImageType.Tiled, 2f);

    #endregion
    #region other

    /// <summary> Adds a toggle also known as checkbox, pretty useful. </summary>
    public Toggle Toggle(string text, Cons<bool> callback) =>
        Builder.Toggle(Resolve("Toggle", 32f), text, 24, white, callback);

    /// <summary> Adds an input field with simple background, although its design is debatable. </summary>
    public InputField Field(string text, Cons<string> callback) =>
        Builder.Field(Resolve("Field", 32f), Tex.Fill, semi, text, 24, callback);

    #endregion
    #region button

    /// <summary> Adds a text button, the most basic kind of buttons. </summary>
    public Button TextButton(string text, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter, Runnable callback = null) =>
        Builder.TextButton(Resolve("TextButton", 40f), Tex.Large, color ?? white, text, 24, align, callback);

    /// <summary> Adds an icon button, the most minimalistic kind of buttons. </summary>
    public Button IconButton(Sprite icon, Color? color = null, Runnable callback = null) =>
        Builder.IconButton(Resolve("IconButton", 40f), Tex.Large, color ?? white, icon, callback);

    /// <summary> Adds a text button, but it's filled with the color. </summary>
    public Button FillButton(string text, Color color, Runnable callback) =>
        Builder.TextButton(Resolve("TextButton", 40f), Tex.Fill, color, text, 24, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds an icon button, but it's filled with the color. </summary>
    public Button FillButton(Sprite icon, Color color, Runnable callback) =>
        Builder.IconButton(Resolve("IconButton", 40f), Tex.Fill, color, icon, callback);

    /// <summary> Adds a text button, but it's made to match the main menu style. </summary>
    public Button MenuButton(string text, Color color, Runnable callback) =>
        Builder.TextButton(Resolve("MenuButton", 75f), Tex.Large, color, text, 36, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds a text button, but it's filled with the color of the given team. </summary>
    public Button TeamButton(Team team, Runnable callback) =>
        Builder.TextButton(Resolve("TeamButton", 80f), Tex.Fill, team.Color(), team == Team.Pink ? "UwU" : "", 24, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds a text button, it opens the profile of the given member. </summary>
    public Button ProfileButton(Friend member, bool full) =>
        Builder.TextButton(Resolve("Profile", full ? 432f : 384f), Tex.Large, Networking.GetTeam(member).Color(), member.Name, 24, TextAnchor.MiddleCenter, () => SteamFriends.OpenUserOverlay(member.Id, "steamid"));

    /// <summary> Adds a button that corresponds to the style of Discord. </summary>
    public Button DiscordButton(string text) =>
        FillButton(text, discord, () => Application.OpenURL("https://discord.com/servers/join-and-kill-em-together-1132614140414935070"));

    /// <summary> Adds a button that corresponds to the style of PayPal. </summary>
    public Button PayPalButton(string text) =>
        FillButton(text, paypal, () => Application.OpenURL("https://www.paypal.com/donate/?hosted_button_id=U5T68JC5LWEMU"));

    /// <summary> Adds a button that corresponds to the style of Buy Me a Coffee. </summary>
    public Button CoffeeButton(string text) =>
        FillButton(text, bmac, () => Application.OpenURL("https://www.buymeacoffee.com/adithedev"));

    #endregion
    #region slider

    /// <summary> Adds a slider, it has no means to display its value. </summary>
    public Slider Slider(int min, int max, Cons<int> callback) =>
        Builder.Slider(Resolve("Slider", 40f), min, max, white, callback);

    /// <summary> Adds a slider, also builds a pair of labels to display the slider value. </summary>
    public Slider Slider(int min, int max, Cons<int> callback, string text, Func<int, string> format)
    {
        Text(text, 32f, out var display);
        return Slider(min, max, value =>
        {
            display.text = format(value);
            callback(value);
        });
    }

    #endregion
    #region scroll

    /// <summary> Adds a scroller, vertical one. </summary>
    public ScrollRect ScrollV(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), voh ? rect.sizeDelta.x - margin * 2f : outerspc, innerspc, false, true);

    /// <summary> Adds a scroller, horizontal one. </summary>
    public ScrollRect ScrollH(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), outerspc, voh ? outerspc : rect.sizeDelta.y - margin * 2f, true, false);

    #endregion
}
