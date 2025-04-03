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

    public Fragment(Transform root, string name, bool dialog, bool woh = false, Prov<bool> cond = null, Runnable hide = null)
    {
        Content = Builder.Canvas(Create(name, root).transform, woh, Dialog = dialog).transform;

        cond ??= () => true;
        hide ??= () => Content.gameObject.SetActive(Shown = false);

        void Check() { if (cond()) hide(); }

        Check();
        Events.OnLoaded += Check;
    }

    /// <summary> Adds a bar, it is located on the left and has a constant width. </summary>
    protected void Bar(float height, Cons<Bar> cons)
    {
        Sidebar ??= Component<Bar>(Builder.Rect("Sidebar", Content, new(240f, 0f, 480f, 0f, new(0f, 0f), new(0f, 1f))).gameObject, b => b.Setup(true, 16f, 16f));

        var img = Sidebar.Image(Sidebar.transform.childCount == 0 ? Tex.Back : Tex.Fill, height, semi, multiplier: 2f).gameObject;

        Component(img, cons);
        Component<HudOpenEffect>(img, e => e.originalSpeed = 40f - Sidebar.transform.childCount * 8f);
    }

    /// <summary> Adds a bar, it is located in the center and has the given size. </summary>
    protected void Bar(float width, float height, Cons<Bar> cons)
    {
        var img = Builder.Image(Builder.Rect("Centerbar", Content, new(width, height)), Tex.Back, semi, ImageType.Sliced, 2f).gameObject;

        Component(img, cons);
        Component<HudOpenEffect>(img, e => e.originalSpeed = 32f);
    }
}
