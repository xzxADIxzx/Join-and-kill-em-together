namespace Jaket.UI.Lib;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary> Adds an image. </summary>
    public Image Image(Sprite sprite, float size, Color? color = null, float? scale = null) =>
        Builder.Image(Resolve("Image", size), sprite, color ?? white, scale);

    /// <summary> Adds a separator. </summary>
    public Image Separator(float size = 16f) =>
        Builder.Image(Resolve("Separator", size), Tex.Dash, red);

    #endregion
    #region other

    /// <summary> Adds a toggle. </summary>
    public Toggle Toggle(string text, Cons<bool> callback, float spc = 32f) =>
        Builder.Toggle(Resolve("Toggle", spc), white, text, 24, callback);

    /// <summary> Adds a field. </summary>
    public InputField Field(string text, Cons<string> callback, float spc = 32f) =>
        Builder.Field(Resolve("Field", spc), Tex.Fill, semi, text, 24, callback);

    #endregion
    #region button

    /// <summary> Adds a text button. </summary>
    public Button TextButton(string text,              Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.BrdL, white, callback, text, 24);

    /// <summary> Adds a text button. </summary>
    public Button TextButton(string text, Color color, Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.BrdL, color, callback, text, 24);

    /// <summary> Adds a icon button. </summary>
    public Button IconButton(Sprite icon,              Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.BrdL, white, callback, icon, 24);

    /// <summary> Adds a icon button. </summary>
    public Button IconButton(Sprite icon, Color color, Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.BrdL, color, callback, icon, 24);

    /// <summary> Adds a fill button. </summary>
    public Button FillButton(string text, Color color, Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.Fill, color, callback, text, 24);

    /// <summary> Adds a fill button. </summary>
    public Button FillButton(Sprite icon, Color color, Runnable callback, float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.Fill, color, callback, icon, 24);

    /// <summary> Adds a team button. </summary>
    public Button TeamButton(Team team,                Runnable callback, float spc = 80f) =>
        Builder.Button(Resolve("Button", spc), Tex.Fill, team.Color(), callback);

    /// <summary> Adds a team button. </summary>
    public Button TeamButton(Friend member,                               float spc = 40f) =>
        Builder.Button(Resolve("Button", spc), Tex.BrdL, member.Team.Color(), () => SteamFriends.OpenUserOverlay(member.Id, "steamid"), member.Name, 24);

    /// <summary> Adds an offset button. </summary>
    public Button OffsetButton(string text, Runnable callback, int size = 24, string value = "")
    {
        Button button = null;
        Subbar(40f, s =>
        {
            s.Setup(false, 0f, 0f);
            s.Text(text, size, spc: s.rect.sizeDelta.x - 120f);
            button = s.TextButton(value, callback, spc: 120f);
        });
        return button;
    }

    /// <summary> Adds a rebind button. </summary>
    public Button RebindButton(Keybind bind, Runnable callback) => OffsetButton(bind.FormatName(), callback, 22, UI.Settings.Rebinding == bind ? "..." : bind.FormatValue());

    #endregion
    #region slider

    /// <summary> Adds a slider. </summary>
    public Slider Slider(int min, int max, Cons<int> callback, string text, Func<int, string> format)
    {
        Pair(text, out var cont);
        cont.text = format(min);

        return Builder.Slider(Resolve("Slider", 40f), white, min, max, value =>
        {
            cont.text = format(value);
            callback(value);
        });
    }

    /// <summary> Adds a slider. </summary>
    public Scrollbar Slider(Bar content) =>
        Builder.Slider(Resolve("Slider", 40f), white, content.GetComponentInParent<ScrollRect>(true));

    /// <summary> Adds a scroll. </summary>
    public ScrollRect ScrollV(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), false, true, voh ? rect.sizeDelta.x - margin * 2f : outerspc, innerspc);

    /// <summary> Adds a scroll. </summary>
    public ScrollRect ScrollH(float innerspc, float outerspc) =>
        Builder.Scroll(Resolve("Scroll", outerspc), true, false, innerspc, voh ? outerspc : rect.sizeDelta.y - margin * 2f);

    #endregion
}
