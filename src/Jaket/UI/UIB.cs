namespace Jaket.UI;

using Steamworks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using ScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode;
using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;

using static Pal;
using static Rect;

/// <summary> Class that builds the entire interface of the mod. </summary>
public class UIB
{
    /// <summary> Various sprites from which the interface is built. </summary>
    public static Sprite Background, Shadows, CircleShadows, Circle, Checkmark;

    /// <summary> Set of colors for buttons. </summary>
    private static ColorBlock colors;
    /// <summary> Block used to get and set material properties. </summary>
    private static MaterialPropertyBlock block = new();

    /// <summary> Loads the resources needed for interface. </summary>
    public static void Load()
    {
        // replace the font in a few in-game fragments
        Action fix;
        Events.OnLoaded += fix = () => Events.Post(() =>
        {
            HudMessageReceiver.Instance.text.font = DollAssets.FontTMP;
            NewMovement.Instance.youDiedText.font = DollAssets.Font;

            // fix the sorting order to display hud messages on top of other interface fragments
            if (!HudMessageReceiver.Instance.TryGetComponent<Canvas>(out _)) Component<Canvas>(HudMessageReceiver.Instance.gameObject, canvas =>
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 2000;
            });
        });
        fix();

        // find all sprites
        var all = Tools.ResFind<Sprite>();
        Sprite Find(string name) => Array.Find(all, s => s.name == name);

        Background = Find("UISprite");
        Shadows = Find("horizontalgradientslowfalloff");
        CircleShadows = Find("weaponwheelbackground");
        Circle = Find("circle");
        Checkmark = Find("pixelx");

        // create a color scheme for buttons
        colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = colors.selectedColor = new(.5f, .5f, .5f, 1f);
        colors.pressedColor = new(1f, 0f, 0f, 1f);
    }

    #region base

    /// <summary> Adds a component to the given object and returns it. Just for convenience. </summary>
    public static T Component<T>(GameObject obj, Action<T> cons) where T : Component
    {
        var c = obj.AddComponent<T>();
        cons(c);
        return c;
    }

    /// <summary> Gets and optionally sets renderer properties. </summary>
    public static void Properties(Renderer renderer, Action<MaterialPropertyBlock> cons, bool set = false)
    {
        renderer.GetPropertyBlock(block);
        cons(block);
        if (set) renderer.SetPropertyBlock(block);
    }

    /// <summary> Adds a rect at the specified position with the given size and anchor. </summary>
    public static RectTransform Rect(string name, Transform parent, Rect r) =>
        Component<RectTransform>(Tools.Create(name, parent), rect =>
        {
            rect.anchorMin = r.Min;
            rect.anchorMax = r.Max;
            rect.anchoredPosition = new(r.x, r.y);
            rect.sizeDelta = new(r.Width, r.Height);
        });

    /// <summary> Adds a translucent black image, that's it. </summary>
    public static Image Table(string name, Transform parent, Rect r, Action<Transform> build = null)
    {
        var image = Image(name, parent, r, new(0f, 0f, 0f, .5f));
        build?.Invoke(image.transform);
        return image;
    }

    /// <summary> Adds a table with a title. </summary>
    public static Image Table(string name, string title, Transform parent, Rect r, Action<Transform> build = null) =>
        Table(name, parent, r, table =>
        {
            Text(title, table, Btn(24f) with { Width = 640f }, size: 32);
            build?.Invoke(table);
        });

    #endregion
    #region canvas

    /// <summary> Creates a canvas that is drawn on top of the camera. </summary>
    public static Transform Canvas(string name, Transform parent, int sort = 1000, float woh = 0f, RenderMode renderMode = RenderMode.ScreenSpaceOverlay, ScaleMode scaleMode = ScaleMode.ScaleWithScreenSize)
    {
        var obj = Tools.Create(name, parent);
        Component<Canvas>(obj, canvas =>
        {
            canvas.renderMode = renderMode;
            canvas.sortingOrder = sort; // move the canvas up, otherwise it will not be visible
        });
        Component<CanvasScaler>(obj, scaler =>
        {
            scaler.uiScaleMode = scaleMode;
            scaler.matchWidthOrHeight = woh;
            scaler.referenceResolution = new(1920f, 1080f);
        });
        return obj.AddComponent<GraphicRaycaster>().transform;
    }

    /// <summary> Creates a canvas that is drawn in world space. </summary>
    public static Transform WorldCanvas(string name, Transform parent, Vector3 position, float scale = .02f, Action<Transform> build = null)
    {
        var canvas = Canvas(name, parent, 0, 0f, RenderMode.WorldSpace, ScaleMode.ConstantPixelSize);
        canvas.localPosition = position;
        canvas.localScale = Vector3.one * scale;

        build?.Invoke(canvas);
        return canvas;
    }

    #endregion
    #region text

    public static Text Text(string name, Transform parent, Rect r, Color? color = null, int size = 24, TextAnchor align = TextAnchor.MiddleCenter) =>
        Component<Text>(Rect("Text", parent, r).gameObject, text =>
        {
            text.text = name.StartsWith("#") ? Bundle.Get(name.Substring(1)) : name;
            text.color = color ?? white;
            text.font = DollAssets.Font;
            text.fontSize = size;
            text.alignment = align;
        });

    #endregion
    #region image

    /// <summary> Adds an image with the given sprite. </summary>
    public static Image Image(string name, Transform parent, Rect r, Color? color = null, Sprite sprite = null, bool fill = true, ImageType type = ImageType.Sliced) =>
        Component<Image>(Rect(name, parent, r).gameObject, image =>
        {
            image.color = color ?? white;
            image.sprite = sprite ?? Background;
            image.fillCenter = fill;
            image.type = type;
        });

    /// <summary> Adds a circular image. </summary>
    public static UICircle CircleImage(string name, Transform parent, Rect r, float arc, int rotation, float thickness, bool outline = false)
    {
        var obj = Rect(name, parent, r).gameObject;
        if (outline) Component<Outline>(obj, outline =>
        {
            outline.effectDistance = new(3f, -3f);
            outline.effectColor = white;
        });
        return Component<UICircle>(obj, circle =>
        {
            circle.Arc = arc;
            circle.ArcRotation = rotation;
            circle.Thickness = thickness;
            circle.Fill = false;
        });
    }

    /// <summary> Adds a diamond-shaped image. </summary>
    public static DiamondGraph DiamondImage(string name, Transform parent, Rect r, float a, float b, float c, float d, Color? color = null) =>
        Component<DiamondGraph>(Rect(name, parent, r).gameObject, diamond =>
        {
            diamond.color = color ?? white;
            diamond.A = a; diamond.B = b; diamond.C = c; diamond.D = d;
        });

    #endregion
    #region button

    /// <summary> Adds a regular button that calls the given action. </summary>
    public static Button Button(string name, Transform parent, Rect r, Color? color = null, int size = 24, TextAnchor align = TextAnchor.MiddleCenter, Action clicked = null)
    {
        var img = Image(name, parent, r, color, fill: false);
        Text(name, img.transform, r.Text, color, size, align);
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.colors = colors;
            button.onClick.AddListener(() => clicked());
        });
    }

    /// <summary> Adds a square button with a char-icon. </summary>
    public static Button IconButton(string icon, Transform parent, Rect r, Color? color = null, Vector3? offset = null, Action clicked = null)
    {
        var btn = Button(icon, parent, r, color, 40, clicked: clicked);
        btn.transform.GetChild(0).localPosition = offset ?? new(.5f, 2f);
        return btn;
    }

    /// <summary> Adds a command button with the appropriate color. </summary>
    public static Button TeamButton(Team team, Transform parent, Rect r, Action clicked = null)
    {
        var img = Image(team.ToString(), parent, r, team.Color());
        if (team == Team.Pink) Text("UwU", img.transform, r.Text, Dark(pink));
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.onClick.AddListener(() => clicked());
        });
    }

    /// <summary> Adds a button that opens the profile of the given member. </summary>
    public static Button ProfileButton(Friend member, Transform parent, Rect r) =>
        Button(member.Name, parent, r, Networking.GetTeam(member).Color(), 24, clicked: () => SteamFriends.OpenUserOverlay(member.Id, "steamid"));

    /// <summary> Adds a button that changes the given keybind. </summary>
    public static Button KeyButton(string name, KeyCode current, Transform parent, Rect r)
    {
        // key is the current keycode, bind is the name of the keybind
        Text key = null, bind = Text("#keybind." + name, parent, r, size: 16, align: TextAnchor.MiddleLeft);

        var img = Table("Button", bind.transform, new(-64f, 0f, 128f, 32f, new(1f, .5f)), table =>
        {
            key = Text(Dialogs.Settings.KeyName(current), table, Size(128f, 32f), size: 16);
        });
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.onClick.AddListener(() => Dialogs.Settings.Instance.Rebind(name, key, img));
        });
    }

    /// <summary> Adds a button corresponding to the Discord style and opening a link to our server. </summary>
    public static Button DiscordButton(string name, Transform parent)
    {
        var img = Image(name, parent, Btn(0f), discord);
        Text(name, img.transform, Huge, size: 240).transform.localScale /= 10f;
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.onClick.AddListener(() => Application.OpenURL("https://discord.gg/USpt3hCBgn"));
        });
    }

    #endregion
    #region toggle

    public static Toggle Toggle(string name, Transform parent, Rect r, int size = 24, Action<bool> clicked = null) =>
        Component<Toggle>(Text(name, parent, r, size: size, align: TextAnchor.MiddleLeft).gameObject, toggle =>
        {
            toggle.onValueChanged.AddListener(_ => clicked(_));

            var br = new Rect(-16f, 0f, 32f, 32f, new(1f, .5f), new(1f, .5f));
            Table("Button", toggle.transform, br, table =>
            {
                toggle.graphic = Image("Checkmark", table, Size(32f, 32f), sprite: Checkmark);
            });
        });

    #endregion
    #region scroll & slider

    /// <summary> Adds an image-mask that cuts off a part of the interface. </summary>
    public static Mask Mask(string name, Transform transform, Rect r) =>
        Component<Mask>(Image(name, transform, r).gameObject, mask => mask.showMaskGraphic = false);

    /// <summary> Adds a scroller with content. </summary>
    public static ScrollRect Scroll(string name, Transform transform, Rect r, float contentWidth = 0f, float contentHeight = 0f, bool horizontal = false, bool vertical = true) =>
        Component<ScrollRect>(Rect(name, transform, r).gameObject, rect =>
        {
            rect.horizontal = horizontal;
            rect.vertical = vertical;

            rect.viewport = Mask("Viewport", rect.transform, Size(r.Width, r.Height)).rectTransform;
            rect.content = Rect("Content", rect.viewport, Size(contentWidth, contentHeight));
            rect.content.pivot = new(.5f, 1f);
        });

    /// <summary> Adds a slider with a handle. </summary>
    public static Slider Slider(string name, Transform parent, Rect r, int max, Action<int> cons = null) =>
        Component<Slider>(Table(name, parent, r).gameObject, slider =>
        {
            slider.fillRect = Image("Fill", slider.transform, new(0f, 0f, 0f, 0f)).rectTransform;
            slider.targetGraphic = Image("Handle", slider.transform, new(0f, 0f, r.Height, 0f));
            slider.handleRect = slider.targetGraphic.rectTransform;

            slider.wholeNumbers = true;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(_ => cons((int)_));
        });

    #endregion
    #region field

    public static InputField Field(string name, Transform parent, Rect r, int size = 24, Action<string> cons = null)
    {
        var tr = new Rect(8f, 1f, r.Width, r.Height);
        var img = Table("Field", parent, r);
        var text = Text("", img.transform, tr, null, size, TextAnchor.MiddleLeft);
        var placeholder = Text(name, img.transform, tr, new(.8f, .8f, .8f, .8f), size, TextAnchor.MiddleLeft);

        return Component<InputField>(img.gameObject, field =>
        {
            field.targetGraphic = img;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.onEndEdit.AddListener(_ => cons(_));
        });
    }

    #endregion
    #region shadow

    /// <summary> Adds a gradient shadow located on the left side. </summary>
    public static Image Shadow(Transform parent) =>
        Image("Shadow", parent, new(160f, 0f, 320f, 4200f, new(0f, .5f), new(0f, .5f)), black, Shadows);

    /// <summary> Adds a circular shadow located in the center. </summary>
    public static UICircle CircleShadow(Transform parent) =>
        Component<UICircle>(Rect("Shadow", parent, Size(640f, 640f)).gameObject, circle =>
        {
            circle.sprite = CircleShadows;
            circle.color = black;
            circle.Thickness = 245f;
            circle.Fill = false;
        });

    #endregion
    #region line

    /// <summary> Adds a line renderer, the size of which will always be equals to the screen. </summary>
    public static UILineRenderer Line(string name, Transform parent, Color? color = null) =>
        Component<UILineRenderer>(Rect(name, parent, new(8f, 8f, 0f, 0f, Vector2.zero, Vector2.zero)).gameObject, line => line.color = color ?? white);

    #endregion
}
