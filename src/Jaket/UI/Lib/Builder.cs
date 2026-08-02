namespace Jaket.UI.Lib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using ScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode;

using Jaket.Assets;

using static Pal;

/// <summary> Set of different tools for building complex interface elements. </summary>
public static class Builder
{
    #region rect

    /// <summary> Creates a rect. </summary>
    public static RectTransform Rect(string name, Transform parent, Rect rect) => Component<RectTransform>(Create(name, parent.transform), rect.Apply);

    /// <summary> Creates a rect. </summary>
    public static RectTransform Rect(string name, Component parent, Rect rect) => Component<RectTransform>(Create(name, parent.transform), rect.Apply);

    #endregion
    #region mask

    /// <summary> Creates a mask. </summary>
    public static Mask Mask(Transform rect, Sprite sprite) =>
        Component<Mask>(rect.gameObject, m =>
        {
            m.showMaskGraphic = false;
            Image(rect, sprite, white);
        });

    #endregion
    #region text

    /// <summary> Creates a text. </summary>
    public static Text Text(Transform rect, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool alignByGeometry = true) =>
        Component<Text>(rect.gameObject, t =>
        {
            t.text = text.StartsWith('#') ? Bundle.Get(text[1..]) : text;
            t.font = ModAssets.DefFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.alignByGeometry = alignByGeometry;
        });

    /// <summary> Creates a pair. </summary>
    public static void Pair(Transform rect, string text, int size, Color color, out Text t1, out Text t2)
    {
        t1 = Text(Rect("Pair", rect, new()), text, size, color, TextAnchor.MiddleLeft);
        t2 = Text(Rect("Pair", rect, new()), text, size, color, TextAnchor.MiddleRight);
    }

    #endregion
    #region image

    /// <summary> Creates an image. </summary>
    public static Image Image(Transform rect, Sprite sprite, Color color, float? scale = null) =>
        Component<Image>(rect.gameObject, i =>
        {
            i.sprite = sprite;
            i.color = color;
            i.type = Tex.Type(sprite);
            i.pixelsPerUnitMultiplier = scale ?? Tex.Scale(sprite);
        });

    /// <summary> Creates an image. </summary>
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

    /// <summary> Creates an image. </summary>
    public static PerfectDiamond Diamond(Transform rect, Color color, float a, float b, float c, float d) =>
        Component<PerfectDiamond>(rect.gameObject, i =>
        {
            i.color = color;
            i.A = a; i.B = b; i.C = c; i.D = d;
        });

    public class PerfectDiamond : DiamondGraph
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            float num = rectTransform.rect.width / 2f;

            vh.AddVert(new(-num * A, 0f), color, new());
            vh.AddVert(new(0f, -num * B), color, new());
            vh.AddVert(new(+num * C, 0f), color, new());
            vh.AddVert(new(0f, +num * D), color, new());
            vh.AddTriangle(4, 5, 6);
            vh.AddTriangle(6, 7, 4);
        }
    }

    public static Image PreserveAspect(this Image image) { image.preserveAspect = true; return image; }

    #endregion
    #region button

    /// <summary> Set of colors for buttons. </summary>
    public static ColorBlock Colors = new()
    {
        normalColor      = white,
        highlightedColor = light,
        pressedColor     = red,
        selectedColor    = light,
        disabledColor    = heavy,

        colorMultiplier  = 1f,
        fadeDuration     = 1f / 12f,
    };

    /// <summary> Creates a button. </summary>
    public static Button Button(Transform rect, Sprite sprite, Color color, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);
        });

    /// <summary> Creates a button. </summary>
    public static Button Button(Transform rect, Sprite sprite, Color color, Runnable callback, string text, int size) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);

            Text(Rect("Text", rect, new()), text, size, sprite == Tex.Fill ? white : color);
        });

    /// <summary> Creates a button. </summary>
    public static Button Button(Transform rect, Sprite sprite, Color color, Runnable callback, Sprite icon, int size) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);

            Image(Rect("Icon", rect, new(size, size)), icon, sprite == Tex.Fill ? white : color);
        });

    #endregion
    #region toggle

    /// <summary> Creates a toggle. </summary>
    public static Toggle Toggle(Transform rect, Color color, string text, int size, Cons<bool> callback) =>
        Component<Toggle>(rect.gameObject, t =>
        {
            var checkbox = Image(Rect("Checkbox",  rect,     new(-16f, 0f, 32f, 32f, new(1f, .5f))), Tex.BrdS, color);
            var checkmrk = Image(Rect("Checkmark", checkbox, new(          16f, 16f              )), Tex.Mark, color);

            static ColorBlock GetColor(bool value) => Colors with { colorMultiplier = value ? 1f : .1f };

            t.targetGraphic = checkmrk;
            t.colors = GetColor(false);
            t.onValueChanged.AddListener(value => t.colors = GetColor(value));
            t.onValueChanged.AddListener(callback.Invoke);

            Text(rect, text, size, color, TextAnchor.MiddleLeft);
        });

    #endregion
    #region slider

    /// <summary> Creates a slider. </summary>
    public static Slider Slider(Transform rect, Color color, int min, int max, Cons<int> callback) =>
        Component<Slider>(rect.gameObject, s =>
        {
            var area = Rect("Area", rect, new(-16f, 0f, -48f, -16f, new(0f, 0f), new(1f, 1f)));
            var mask = Rect("Mask", area, new());
            var fill = Rect("Fill", mask, new());
            var zone = Rect("Zone", rect, new(0.0f, 0f, -48f, -16f, new(0f, 0f), new(1f, 1f)));
            var hand = Rect("Hand", zone, new(32f, 0f));

            Image(rect, Tex.BrdL, color);
            Mask (mask, Tex.Mask       );
            Image(fill, Tex.Dash, heavy);

            s.targetGraphic = Image(hand, Tex.Hort, color);
            s.colors = Colors;
            s.fillRect = mask;
            s.handleRect = hand;

            s.wholeNumbers = true;
            s.minValue = min;
            s.maxValue = max;
            s.onValueChanged.AddListener(value => callback((int)value));
        });

    /// <summary> Creates a slider. </summary>
    public static Scrollbar Slider(Transform rect, Color color, ScrollRect scroll) =>
        Component<Scrollbar>(rect.gameObject, s =>
        {
            var zone = Rect("Zone", rect, new(0f, 0f, -16f, -48f, new(0f, 0f), new(1f, 1f)));
            var hand = Rect("Hand", zone, new(0f, 32f));

            Image(rect, Tex.BrdL, color);

            s.targetGraphic = Image(hand, Tex.Vert, color);
            s.colors = Colors;
            s.handleRect = hand;
            s.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = s;
        });

    /// <summary> Creates a scroll. </summary>
    public static ScrollRect Scroll(Transform rect, bool horizontal, bool vertical, float width, float height) =>
        Component<ScrollRect>(rect.gameObject, s =>
        {
            s.horizontal = horizontal;
            s.vertical = vertical;

            s.viewport = Mask(rect, null).rectTransform;
            s.content = Rect("Content", s.viewport, new(width, height));
        });

    #endregion
    #region field

    /// <summary> Creates a field. </summary>
    public static InputField Field(Transform rect, Sprite sprite, Color color, string ph, int size, Cons<string> callback) =>
        Component<InputField>(rect.gameObject, f =>
        {
            f.targetGraphic = Image(rect, sprite, color);
            f.textComponent = Text(Rect("Textfield", rect, new() { Width = -16f, Y = 3f }), "", size, white, TextAnchor.MiddleLeft, false);
            f.placeholder = Text(Rect("Placeholder", rect, new() { Width = -16f, Y = 3f }), ph, size, light, TextAnchor.MiddleLeft, false);
            f.onEndEdit.AddListener(callback.Invoke);
        });

    #endregion
    #region canvas

    /// <summary> Creates a canvas. </summary>
    public static Canvas Canvas(Transform rect, bool touchable) =>
        Component<Canvas>(rect.gameObject, c => (rect = c.transform).Component<CanvasScaler>(s =>
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            s.uiScaleMode = ScaleMode.ScaleWithScreenSize;

            c.sortingOrder = 42;
            s.matchWidthOrHeight = 1f;
            s.referenceResolution = new(1920f, 1080f);

            if (touchable) rect.Component<GraphicRaycaster>(_ => { });
        }));

    /// <summary> Creates a canvas. </summary>
    public static Canvas Canvas(Transform rect, Vector3 position, Cons<Transform> cons) =>
        Component<Canvas>(rect.gameObject, c => (rect = c.transform).Component<CanvasScaler>(s =>
        {
            c.renderMode = RenderMode.WorldSpace;
            s.uiScaleMode = ScaleMode.ConstantPixelSize;

            c.sortingOrder = 1;
            rect.localPosition = position;
            rect.localScale = Vector3.one * .002f;

            cons(rect);
        }));

    #endregion
}
