namespace Jaket.UI.Lib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using ImageType = UnityEngine.UI.Image.Type;
using ScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode;

using Jaket.Assets;

using static Pal;

/// <summary> Set of different tools for building complex interface elements. </summary>
public static class Builder
{
    #region base

    /// <summary> Set of colors for buttons. </summary>
    public static ColorBlock Colors = new()
    {
        normalColor = white,
        pressedColor = red,
        highlightedColor = gray,
        selectedColor = gray,
        disabledColor = dark,

        colorMultiplier = 1f,
        fadeDuration = .1f
    };

    /// <summary> Creates a rect with the specified position, size and alignment. </summary>
    public static RectTransform Rect(string name, Transform parent, Rect rect) => Component<RectTransform>(Create(name, parent), rect.Apply);

    /// <summary> Creates a mask that cuts off a part of the interface. </summary>
    public static Mask Mask(Transform rect, Sprite sprite) =>
        Component<Mask>(rect.gameObject, m =>
        {
            m.showMaskGraphic = false;
            Image(rect, sprite, white, ImageType.Sliced);
        });

    /// <summary> Creates a label with the given text, size, color and alignment. </summary>
    public static Text Text(Transform rect, string text, int size, Color color, TextAnchor align, bool alignByGeometry = true) =>
        Component<Text>(rect.gameObject, t =>
        {
            t.text = text == string.Empty || text[0] != '#' ? text : Bundle.Get(text[1..]);
            t.font = ModAssets.DefFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.alignByGeometry = alignByGeometry;
        });

    #endregion
    #region image

    /// <summary> Creates an image with the given sprite, color and type. </summary>
    public static Image Image(Transform rect, Sprite sprite, Color color, ImageType type, float? multiplier = null) =>
        Component<Image>(rect.gameObject, i =>
        {
            i.sprite = sprite;
            i.color = color;
            i.type = type;
            i.pixelsPerUnitMultiplier = multiplier ?? Tex.Scale(sprite);
        });

    /// <summary> Creates a circular image with the given parameters. </summary>
    public static UICircle Circle(Transform rect, float arc, int rotation, float thickness, Sprite sprite = null, Color? color = null) =>
        Component<UICircle>(rect.gameObject, i =>
        {
            i.sprite = sprite;
            i.color = color ?? white;
            i.Arc = arc;
            i.ArcRotation = rotation;
            i.Thickness = thickness;
            i.Fill = false;
        });

    /// <summary> Adds a diamond-shaped image with the given parameters. </summary>
    public static DiamondGraph Diamond(Transform rect, Color color, float a, float b, float c, float d, bool culloff = false) =>
        Component<DiamondGraph>(rect.gameObject, i =>
        {
            i.color = color;
            i.A = a; i.B = b; i.C = c; i.D = d;

            if (culloff) Component<PerfectDiamond>(rect.gameObject, p => p.Diamond = i);
        });

    /// <summary> Makes the given diamond be visible from both sides. </summary>
    public class PerfectDiamond : MonoBehaviour, IMeshModifier
    {
        /// <summary> Diamond to modify. </summary>
        public DiamondGraph Diamond;

        public void ModifyMesh(Mesh mesh) { }

        public void ModifyMesh(VertexHelper vh)
        {
            float num = Diamond.rectTransform.rect.width / 2f;

            vh.AddVert(new(-num * Diamond.A, 0f), Diamond.color, new());
            vh.AddVert(new(0f, -num * Diamond.B), Diamond.color, new());
            vh.AddVert(new(+num * Diamond.C, 0f), Diamond.color, new());
            vh.AddVert(new(0f, +num * Diamond.D), Diamond.color, new());
            vh.AddTriangle(4, 5, 6);
            vh.AddTriangle(6, 7, 4);
        }
    }

    #endregion
    #region button

    /// <summary> Creates a button with the given sprite, color and callback. </summary>
    public static Button Button(Transform rect, Sprite sprite, Color color, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);
        });

    /// <summary> Creates a button with the given sprite, color, text, size, alignment and callback. </summary>
    public static Button TextButton(Transform rect, Sprite sprite, Color color, string text, int size, TextAnchor align, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);

            Text(Rect("Text", rect, Lib.Rect.Fill), text, size, sprite == Tex.Fill ? white : color, align);
        });

    /// <summary> Creates a button with the given sprite, color, icon and callback. </summary>
    public static Button IconButton(Transform rect, Sprite sprite, Color color, Sprite icon, int size, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);

            Image(Rect("Icon", rect, new(size, size)), icon, sprite == Tex.Fill ? white : color, ImageType.Simple);
        });

    #endregion
    #region toggle

    /// <summary> Creates a color block with transparency based on the given toggle state. </summary>
    public static ColorBlock GetColor(bool active) => Colors with { colorMultiplier = active ? 1f : .3f };

    /// <summary> Creates a toggle with the given text, size, color and callback. </summary>
    public static Toggle Toggle(Transform rect, string text, int size, Color color, Cons<bool> callback) =>
        Component<Toggle>(rect.gameObject, t =>
        {
            var subrect1 = Rect("Checkbox", rect, new(-16f, 0f, 32f, 32f, new(1f, .5f)));
            var checkbox = Image(subrect1, Tex.Small, color, ImageType.Sliced);

            var subrect2 = Rect("Checkmark", subrect1, new(16f, 16f));
            var checkmrk = Image(subrect2, Tex.Mark, color, ImageType.Simple);

            t.targetGraphic = checkmrk;
            t.colors = GetColor(false);
            t.onValueChanged.AddListener(value => t.colors = GetColor(value));
            t.onValueChanged.AddListener(callback.Invoke);

            Text(rect, text, size, color, TextAnchor.MiddleLeft);
        });

    #endregion
    #region slider

    /// <summary> Creates a slider with the given range, color and callback. </summary>
    public static Slider Slider(Transform rect, int min, int max, Color color, Cons<int> callback) =>
        Component<Slider>(rect.gameObject, s =>
        {
            var area = Rect("Area", rect, new(-16f, 0f, -48f, -16f, Vector2.zero, Vector2.one));
            var mask = Rect("Mask", area, Lib.Rect.Fill);
            var fill = Rect("Fill", mask, Lib.Rect.Fill);

            Mask(mask, Tex.Mask);
            Image(fill, Tex.Dash, dark, ImageType.Tiled);
            s.fillRect = mask;

            var zone = Rect("Zone", rect, new(0f, 0f, -48f, -16f, Vector2.zero, Vector2.one));
            var hand = Rect("Hand", zone, Lib.Rect.Fill with { Width = 32f });

            s.targetGraphic = Image(hand, Tex.Hort, color, ImageType.Sliced);
            s.colors = Colors;
            s.handleRect = hand;

            s.wholeNumbers = true;
            s.minValue = min;
            s.maxValue = max;
            s.onValueChanged.AddListener(_ => callback((int)_));

            Image(rect, Tex.Large, color, ImageType.Sliced);
        });

    /// <summary> Creates a slider with the given color and scroll rect to control. </summary>
    public static Scrollbar Slider(Transform rect, Color color, ScrollRect scroll) =>
        Component<Scrollbar>(rect.gameObject, s =>
        {
            var zone = Rect("Zone", rect, new(0f, 0f, -16f, -48f, Vector2.zero, Vector2.one));
            var hand = Rect("Hand", zone, Lib.Rect.Fill with { Height = 32f });

            s.targetGraphic = Image(hand, Tex.Vert, color, ImageType.Sliced);
            s.colors = Colors;
            s.handleRect = hand;
            s.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = s;

            Image(rect, Tex.Large, color, ImageType.Sliced);
        });

    #endregion
    #region scroll

    /// <summary> Creates a scroller with the given content size and scroll direction. Does not assign sliders or create any. </summary>
    public static ScrollRect Scroll(Transform rect, float width, float height, bool horizontal, bool vertical) =>
        Component<ScrollRect>(rect.gameObject, s =>
        {
            s.horizontal = horizontal;
            s.vertical = vertical;

            s.viewport = Mask(rect, null).rectTransform;
            s.content = Rect("Content", s.viewport, new(0f, 0f, width, height));
        });

    #endregion
    #region field

    /// <summary> Creates a singleline input field with the given sprite, color, placeholder, size and callback. </summary>
    public static InputField Field(Transform rect, Sprite sprite, Color color, string ph, int size, Cons<string> callback) =>
        Component<InputField>(rect.gameObject, f =>
        {
            f.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            f.textComponent = Text(Rect("Textfield", rect, Lib.Rect.Fill with { Width = -16f, Y = 3f }), "", size, white, TextAnchor.MiddleLeft, false);
            f.placeholder = Text(Rect("Placeholder", rect, Lib.Rect.Fill with { Width = -16f, Y = 3f }), ph, size, light, TextAnchor.MiddleLeft, false);
            f.onEndEdit.AddListener(callback.Invoke);
        });

    #endregion
    #region canvas

    /// <summary> Creates a canvas that is drawn on top of the main camera. </summary>
    public static Canvas Canvas(Transform rect, bool touchable) =>
        Component<Canvas>(rect.gameObject, c => Component<CanvasScaler>((rect = c.transform).gameObject, s =>
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            s.uiScaleMode = ScaleMode.ScaleWithScreenSize;

            c.sortingOrder = 4200; // move the canvas up, otherwise it won't be visible
            s.matchWidthOrHeight = 1f;
            s.referenceResolution = new(1920f, 1080f);

            if (touchable) rect.gameObject.AddComponent<GraphicRaycaster>();
        }));

    /// <summary> Creates a canvas that is drawn in the world space. </summary>
    public static Canvas WorldCanvas(Transform rect, Vector3 position, Cons<Transform> cons) =>
        Component<Canvas>(rect.gameObject, c => Component<CanvasScaler>((rect = c.transform).gameObject, s =>
        {
            c.renderMode = RenderMode.WorldSpace;
            s.uiScaleMode = ScaleMode.ConstantPixelSize;

            c.sortingOrder = 1; // there's no need to move the canvas too high
            rect.localPosition = position;
            rect.localScale = Vector3.one * .002f;

            cons(rect);
        }));

    #endregion
}
