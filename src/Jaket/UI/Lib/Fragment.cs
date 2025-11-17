namespace Jaket.UI.Lib;

using UnityEngine;

using ImageType = UnityEngine.UI.Image.Type;

using static Pal;

/// <summary> Interface fragment that contains multiple tables. </summary>
public class Fragment
{
    /// <summary> Whether the fragment is a dialog. </summary>
    public bool Dialog { get; private set; }
    /// <summary> Whether the fragment is visible. </summary>
    public bool Shown;

    /// <summary> Transform containing the content of the fragment. </summary>
    public Transform Content;
    /// <summary> Vertical bar located on the left side of the fragment. </summary>
    public Bar Sidebar;

    public Fragment(Transform root, string name, bool dialog, Prov<bool> cond = null, Runnable hide = null)
    {
        Content = Builder.Canvas(Create(name, root).transform, Dialog = dialog).transform;

        cond ??= () => true;
        hide ??= () => Content.gameObject.SetActive(Shown = false);

        void Check() { if (cond()) hide(); }

        Events.Post(Check);
        Events.OnLoad += Check;
    }

    /// <summary> Toggles visibility of the fragment. </summary>
    public virtual void Toggle() => Content.gameObject.SetActive(Shown = !Shown);

    /// <summary> Rebuilds the fragment if possible. </summary>
    public virtual void Rebuild() { }

    #region building

    /// <summary> Adds a rectangle, it is not preconfigured. </summary>
    protected RectTransform Rect(string name, Rect rect) => Builder.Rect(name, Content, rect);

    /// <summary> Adds a rectangle, it fills the fragment. </summary>
    protected RectTransform Fill(string name) => Builder.Rect(name, Content, Lib.Rect.Fill);

    /// <summary> Adds a bar, it is located on the left and has a constant width. </summary>
    protected void Bar(float height, Cons<Bar> cons)
    {
        Sidebar ??= Component<Bar>(Rect("Sidebar", new(256f, 0f, 480f, 0f, new(0f, 0f), new(0f, 1f))).gameObject, b => b.Setup(true, 16f, 16f));

        var img = Sidebar.Image(Sidebar.Empty ? Tex.Back : Tex.Fill, height, semi, multiplier: Sidebar.Empty ? 2f : 3f).gameObject;

        Component(img, cons);
        Component<HudOpenEffect>(img, e => e.speed = 38f - height / 24f);

        if (Content.Find("Deco") == null) Builder.Image(Rect("Deco", new(0f, 0f, 32f, 0f, new(0f, 0f), new(0f, 1f))), Tex.Dash, semi, ImageType.Tiled, 2f);
    }

    /// <summary> Adds a bar, it is located in the center and has the given size. </summary>
    protected void Bar(float width, float height, Cons<Bar> cons)
    {
        var img = Builder.Image(Rect("Centerbar", new(width, height)), Tex.Back, semi, ImageType.Sliced, 2f).gameObject;

        Component(img, cons);
        Component<HudOpenEffect>(img, e => e.speed = 32f);

        if (Content.Find("Deco") == null) Builder.Image(Rect("Deco", new(width + 24f, height + 24f)), Tex.Large, semi, ImageType.Sliced, 2f).raycastTarget = false;

        Builder.IconButton(Builder.Rect("Close", img.transform, new(-24f, -24f, 32f, 32f, Vector2.one)), Tex.Fill, red, Tex.Mark, 16, Toggle);
    }

    /// <summary> Adds a bar, it displays the current version of the project. </summary>
    protected void VersionBar()
    {
        var bar = Builder.Rect("Version", Sidebar.transform, new(0f, 36f, 480f - 36f, 40f, new(.5f, 0f)));
        var txt = Builder.Rect("Text", bar, Lib.Rect.Fill);

        Builder.Image(bar, Tex.Fill, semi, ImageType.Sliced, 3f);
        Builder.Text(txt, $"Jaket version is {Version.CURRENT}{(Version.DEBUG ? "-beta" : "")}", 24, gray, TextAnchor.MiddleCenter);
    }

    #endregion
}
