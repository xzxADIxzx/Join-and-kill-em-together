namespace Jaket.UI;

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.Content;

/// <summary> Class that builds the entire interface of the mod. </summary>
public class UI
{
    /// <summary> Various sprites from which the interface is built. </summary>
    private static Sprite background, shadow, circleShadow, circle;
    /// <summary> Set of colors for buttons. </summary>
    private static ColorBlock colors;

    /// <summary> Loads the resources needed for interface. </summary>
    public static void Load()
    {
        // find all sprites
        var all = Resources.FindObjectsOfTypeAll<Sprite>();
        Func<string, Sprite> find = name => Array.Find(all, s => s.name == name);

        background = find("UISprite");
        shadow = find("horizontalgradientslowfalloff");
        circleShadow = find("weaponwheelbackground");
        circle = find("circle");

        // create a color scheme for buttons
        colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = colors.selectedColor = new(.5f, .5f, .5f, 1f);
        colors.pressedColor = new(1f, 0f, 0f, 1f);
    }

    /// <summary> Creates singleton instances of various UI elements. </summary>
    public static void Build()
    {
        PlayerList.Build();
        PlayerIndicators.Build();

        Chat.Build();
        EmojiWheel.Build();
    }

    #region base

    /// <summary> Adds a component to the given object and returns it. Just for convenience. </summary>
    public static T Component<T>(GameObject obj, Action<T> action) where T : Component
    {
        var component = obj.AddComponent<T>();
        action.Invoke(component);
        return component;
    }

    /// <summary> Creates a new game object and assigns it to the given transform. </summary>
    public static GameObject Object(string name, Transform parent)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent);
        return obj;
    }

    /// <summary> Creates a translucent black image, that's it. </summary>
    public static Image Table(string name, Transform parent, float x, float y, float width, float height, Action<Transform> action = null)
    {
        var image = Image(name, parent, x, y, width, height, new Color(0f, 0f, 0f, .5f));
        action?.Invoke(image.transform);
        return image;
    }

    /// <summary> Creates a new rect at the specified position with the given size. </summary>
    public static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height) =>
        Component<RectTransform>(Object(name, parent), rect =>
        {
            rect.anchoredPosition = new(x, y);
            rect.sizeDelta = new(width, height);
        });

    #endregion
    #region canvas

    /// <summary> Creates a canvas that is drawn on top of the camera. </summary>
    public static GameObject Canvas(string name, Transform parent)
    {
        var obj = Object(name, parent);
        Component<Canvas>(obj, canvas =>
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // move the canvas up, otherwise it will not be visible
        });
        Component<CanvasScaler>(obj, scaler =>
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920f, 1080f);
        });
        return obj.AddComponent<GraphicRaycaster>().gameObject;
    }

    #endregion
    #region text & image

    /// <summary> Adds text with the given parameters to the canvas. </summary>
    public static Text Text(string name, Transform parent, float x, float y, float width = 320f, float height = 48f,
                            Color? color = null, int size = 32, TextAnchor align = TextAnchor.MiddleCenter) =>
        Component<Text>(Rect("Text", parent, x, y, width, height).gameObject, text =>
        {
            text.text = name;
            text.color = color ?? Color.white;
            text.font = DollAssets.Font;
            text.fontSize = size;
            text.alignment = align;
        });

    /// <summary> Adds an image with the standard sprite to the canvas. </summary>
    public static Image Image(string name, Transform parent, float x, float y, float width = 320f, float height = 48f,
                              Color? color = null, bool fill = true, bool circle = false) =>
        Component<Image>(Rect(name, parent, x, y, width, height).gameObject, image =>
        {
            image.color = color ?? Color.white;
            image.fillCenter = fill;
            image.sprite = circle ? UI.circle : background;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        });

    /// <summary> Adds a circular image with the standard sprite to the canvas. </summary>
    public static UICircle CircleImage(string name, Transform parent, float width, float height, float arc, int rotation, float thickness, bool outline = false)
    {
        var obj = Rect(name, parent, 0f, 0f, width, height).gameObject;
        if (outline) Component<Outline>(obj, outline =>
        {
            outline.effectDistance = new(3f, -3f);
            outline.effectColor = Color.white;
        });
        return Component<UICircle>(obj, circle =>
        {
            circle.Arc = arc;
            circle.ArcRotation = rotation;
            circle.Thickness = thickness;
            circle.Fill = false;
        });
    }

    #endregion
    #region button

    /// <summary> Creates a regular button that calls the given action. </summary>
    public static Button Button(string name, Transform parent, float x, float y, float width = 320f, float height = 48f,
                                Color? color = null, int size = 32, TextAnchor align = TextAnchor.MiddleCenter, UnityAction clicked = null)
    {
        var img = Image(name, parent, x, y, width, height, color, false);
        Text(name, img.transform, 0f, 0f, width, height, color, size, align);
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.colors = colors;
            button.onClick.AddListener(clicked);
        });
    }

    /// <summary> Creates a command button with the appropriate color. </summary>
    public static Button TeamButton(Team team, Transform parent, float x, float y, float width = 51f, float height = 51f, UnityAction clicked = null)
    {
        var img = Image(team.ToString(), parent, x, y, width, height, team.Data().Color());
        if (team == Team.Pink) Text("UwU", img.transform, 0f, 0f, width, height, size: 24);
        return Component<Button>(img.gameObject, button =>
        {
            button.targetGraphic = img;
            button.onClick.AddListener(clicked);
        });
    }

    #endregion
    #region field

    /// <summary> Creates an input field with a placeholder. </summary>
    public static InputField Field(string name, Transform parent, float x, float y, float width = 1920f - 32f, float height = 32f,
                                   int size = 24, UnityAction<string> enter = null)
    {
        var img = Table("Field", parent, x, y, width, height);
        var text = Text("", img.transform, 8f, 1f, width, height, null, size, TextAnchor.MiddleLeft);
        var placeholder = Text(name, img.transform, 8f, 1f, width, height, new Color(.8f, .8f, .8f, .8f), size, TextAnchor.MiddleLeft);

        return Component<InputField>(img.gameObject, field =>
        {
            field.targetGraphic = img;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.onEndEdit.AddListener(enter);
        });
    }

    #endregion
    #region shadow

    /// <summary> Adds a shadow located on the left by default. </summary>
    public static Image Shadow(string name, Transform parent, float x = -800f, float y = 0f, float width = 320f, float height = 2000f) =>
        Component<Image>(Rect(name, parent, x, y, width, height).gameObject, image =>
        {
            image.sprite = shadow;
            image.color = Color.black;
        });

    /// <summary> Adds a circular shadow located in the center by default. </summary>
    public static UICircle CircleShadow(string name, Transform parent, float x = 0f, float y = 0f, float diameter = 640f, float thickness = 245f) =>
        Component<UICircle>(Rect(name, parent, x, y, diameter, diameter).gameObject, circle =>
        {
            circle.sprite = circleShadow;
            circle.color = Color.black;

            circle.Fill = false;
            circle.Thickness = thickness;
        });

    #endregion
}
