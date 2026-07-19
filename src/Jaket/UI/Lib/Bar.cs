namespace Jaket.UI.Lib;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Content;
using Jaket.Input;

using static Pal;

/// <summary> Either horizontal or vertical bar that gets filled with interface elements. </summary>
public class Bar : MonoBehaviour
{
    /// <summary> Whether the bar is vertical or horizontal. </summary>
    private bool voh;
    /// <summary> Margin from the borders and padding between the elements. </summary>
    private float margin, padding;
    /// <summary> Number of pixels claimed by elements. </summary>
    private float filled;
    /// <summary> Action to be done in the update loop. </summary>
    private Runnable update;
    /// <summary> Rectangle that contains this element. </summary>
    private RectTransform rect;

    #region basic

    /// <summary> Sets up the basic options of the bar. </summary>
    public void Setup(bool voh, float margin = 8f, float padding = 8f)
    {
        TryGetComponent(out rect);
        this.voh = voh;
        this.margin = margin;
        this.padding = padding;
    }

    /// <summary> Removes the child elements of the bar. </summary>
    public void Clear()
    {
        rect.Each(Dest);
        filled = 0f;
    }

    private void Update() => update?.Invoke();

    public void Update(Runnable update) => this.update = update;

    #endregion
    #region rect

    /// <summary> Resolves the size of an element and returns a rectangle to build the element in. </summary>
    public RectTransform Resolve(string name, float size)
    {
        float fill = (voh ? rect.sizeDelta.x : rect.sizeDelta.y) - margin * 2f;
        float grow = (filled == 0f ? margin : padding) + size / 2f;

        var result = Builder.Rect(name, rect, new
        (
            voh ? 0f : filled += grow,
            voh ? filled -= grow : 0f,
            voh ? fill : size,
            voh ? size : fill,
            voh ? new(.5f, 1f) : new(0f, .5f)
        ));

        filled += size / (voh ? -2f : 2f);
        return result;
    }

    /// <summary> Adds a space, an utter waste of space. </summary>
    public void Space(float size = 16f) => Resolve("Space", size);

    /// <summary> Adds a subbar, does not configure it. </summary>
    public void Subbar(float size, Cons<Bar> cons) => Resolve("Subbar", size).Component(cons);

    #endregion
    #region text

    /// <summary> Adds a text. </summary>
    public Text Title(string text, float spc = 32f) =>
        Builder.Text(Resolve("Text", spc), text, 32, white, TextAnchor.MiddleCenter);

    /// <summary> Adds a text. </summary>
    public Text Info(string text, float spc = 16f) =>
        Builder.Text(Resolve("Text", spc), text, 16, light, TextAnchor.MiddleLeft);

    /// <summary> Adds a text. </summary>
    public Text Text(string text, int size = 24, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, float spc = 24f) =>
        Builder.Text(Resolve("Text", spc), text, size, color ?? white, align);

    /// <summary> Adds a pair. </summary>
    public void Pair(string text, out Text cont, Color? color = null, float spc = 24f) =>
        Builder.Pair(Resolve("Pair", spc), text, 24, color ?? white, out _, out cont);

    #endregion
    #region image

    /// <summary> Adds an image with the given sprite, rarely used. </summary>
    public Image Image(Sprite sprite, float spc, Color? color = null, ImageType type = ImageType.Sliced, float? multiplier = null) =>
        Builder.Image(Resolve("Image", spc), sprite, color ?? white, type, multiplier);

    /// <summary> Adds an image separator, rarely used. </summary>
    public Image Separator() =>
        Builder.Image(Resolve("Separator", 16f), Tex.Dash, red, ImageType.Tiled);

    #endregion
    #region other

    /// <summary> Adds a toggle also known as checkbox, pretty useful. </summary>
    public Toggle Toggle(string text, Cons<bool> callback, float spc = 32f) =>
        Builder.Toggle(Resolve("Toggle", spc), text, 24, white, callback);

    /// <summary> Adds an input field with simple background, although its design is debatable. </summary>
    public InputField Field(string text, Cons<string> callback, float spc = 32f) =>
        Builder.Field(Resolve("Field", spc), Tex.Fill, semi, text, 24, callback);

    #endregion
    #region button

    /// <summary> Adds a text button, the most basic kind of buttons. </summary>
    public Button TextButton(string text, Color? color = null, Runnable callback = null, TextAnchor align = TextAnchor.MiddleCenter, float spc = 40f) =>
        Builder.TextButton(Resolve("TextButton", spc), Tex.Large, color ?? white, text, 24, align, callback);

    /// <summary> Adds an icon button, the most minimalistic kind of buttons. </summary>
    public Button IconButton(Sprite icon, Color? color = null, Runnable callback = null) =>
        Builder.IconButton(Resolve("IconButton", 40f), Tex.Large, color ?? white, icon, 24, callback);

    /// <summary> Adds a text button, but it's filled with the color. </summary>
    public Button FillButton(string text, Color color, Runnable callback) =>
        Builder.TextButton(Resolve("TextButton", 40f), Tex.Fill, color, text, 24, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds an icon button, but it's filled with the color. </summary>
    public Button FillButton(Sprite icon, Color color, Runnable callback) =>
        Builder.IconButton(Resolve("IconButton", 40f), Tex.Fill, color, icon, 24, callback);

    /// <summary> Adds a text button, but it's made to match the main menu style. </summary>
    public Button MenuButton(string text, Color color, Runnable callback) =>
        Builder.TextButton(Resolve("MenuButton", 75f), Tex.Large, color, text, 36, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds a text button, but it's filled with the color of the given team. </summary>
    public Button TeamButton(Team team, Runnable callback) =>
        Builder.TextButton(Resolve("TeamButton", 80f), Tex.Fill, team.Color(), team == Team.Pink ? "UwU" : "", 24, TextAnchor.MiddleCenter, callback);

    /// <summary> Adds a text button, it opens the profile of the given member. </summary>
    public Button ProfileButton(Friend member, bool full) =>
        Builder.TextButton(Resolve("Profile", full ? 432f : 384f), Tex.Large, member.Team.Color(), member.Name, 24, TextAnchor.MiddleCenter, () => SteamFriends.OpenUserOverlay(member.Id, "steamid"));

    /// <summary> Adds a button that corresponds to the style of Discord. </summary>
    public Button DiscordButton(string text) =>
        FillButton(text, discord, () => Application.OpenURL("https://discord.com/servers/join-and-kill-em-together-1132614140414935070"));

    /// <summary> Adds a button, it has a label on the left. </summary>
    public Button OffsetButton(string text, Runnable callback, int size = 24, string value = "")
    {
        Button button = null;
        Subbar(40f, s =>
        {
            s.Setup(false, 0f, 0f);
            s.Text(text, s.rect.sizeDelta.x - 120f, size, align: TextAnchor.MiddleLeft);
            button = s.TextButton(value, spc: 120f, callback: callback);
        });
        return button;
    }

    /// <summary> Adds a button, it displays and rebinds the given keybind.</summary>
    public Button RebindButton(Keybind bind, Runnable callback) =>
        OffsetButton(bind.FormatName(), callback, 22, UI.Settings.Rebinding == bind ? "..." : bind.FormatValue());

    #endregion
    #region slider

    /// <summary> Adds a slider, it has no means to display its value. </summary>
    public Slider Slider(int min, int max, Cons<int> callback) =>
        Builder.Slider(Resolve("Slider", 40f), min, max, white, callback);

    /// <summary> Adds a slider, also builds a pair of labels to display the slider value. </summary>
    public Slider Slider(int min, int max, Cons<int> callback, string text, Func<int, string> format)
    {
        Pair(text, out var display);
        display.text = format(0);
        return Slider(min, max, value =>
        {
            display.text = format(value);
            callback(value);
        });
    }

    /// <summary> Adds a slider, it controls the given scroll rect. </summary>
    public Scrollbar Slider(ScrollRect scroll) =>
        Builder.Slider(Resolve("Slider", 40f), white, scroll);

    /// <summary> Adds a slider, it controls the scroll rect containing the given content. </summary>
    public Scrollbar Slider(Transform content) =>
        Builder.Slider(Resolve("Slider", 40f), white, content.GetComponentInParent<ScrollRect>(true));

    #endregion
    #region scroll

    /// <summary> Adds a scroller, vertical one. </summary>
    public ScrollRect ScrollV(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), voh ? rect.sizeDelta.x - margin * 2f : outerspc, innerspc, false, true);

    /// <summary> Adds a scroller, horizontal one. </summary>
    public ScrollRect ScrollH(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), innerspc, voh ? outerspc : rect.sizeDelta.y - margin * 2f, true, false);

    #endregion
}
