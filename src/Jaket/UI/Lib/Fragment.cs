namespace Jaket.UI.Lib;

using UnityEngine;

/// <summary> Interface fragment that contains multiple tables. </summary>
public class Fragment
{
    /// <summary> Whether the fragment is a dialog. </summary>
    public bool Dialog { get; private set; }
    /// <summary> Whether the fragment is visible. </summary>
    public bool Shown;

    /// <summary> Transform containing the content of the fragment. </summary>
    public Transform Content;

    public Fragment(Transform root, string name, bool dialog, bool woh = false, Prov<bool> cond = null, Runnable hide = null)
    {
        Builder.Canvas(Content = Create(name, root).transform, woh, Dialog = dialog);

        cond ??= () => true;
        hide ??= () => Content.gameObject.SetActive(Shown = false);

        void Check() { if (cond()) hide(); }

        Check();
        Events.OnLoaded += Check;
    }
}
