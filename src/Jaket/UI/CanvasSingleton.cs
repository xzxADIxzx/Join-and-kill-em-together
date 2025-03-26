namespace Jaket.UI;

using UnityEngine.UI;

/// <summary> Singleton based on canvas. Used for interface construction. </summary>
public class CanvasSingleton<T> : MonoSingleton<T> where T : CanvasSingleton<T>
{
    /// <summary> Whether the canvas is a dialog or fragment. </summary>
    public static bool Dialog { get; private set; }
    /// <summary> Whether the canvas is visible or hidden. </summary>
    public static bool Shown;

    /// <summary> Creates an instance of this singleton. </summary>
    /// <param name="woh"> Width or height will be used to scale the canvas. True - width, false - height. </param>
    /// <param name="dialog"> Dialogs lock the mouse and movement while fragments don't do this. </param>
    public static void Build(string name, bool dialog, Prov<bool> cond = null, Runnable hide = null, bool woh = false)
    {
        UIB.Canvas(name, UI.Root, woh: woh ? 0f : 1f).gameObject.AddComponent<T>();
        Instance.GetComponent<GraphicRaycaster>().enabled = Dialog = dialog;

        cond ??= () => true;
        hide ??= () => Instance.gameObject.SetActive(Shown = false);

        void Check() { if (cond()) hide(); }

        Check();
        Events.OnLoaded += Check;
    }
}
