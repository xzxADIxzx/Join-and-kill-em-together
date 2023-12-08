namespace Jaket.UI;

using System;

using Jaket.World;

/// <summary> Singleton based on canvas. Used for interface construction. </summary>
public class CanvasSingleton<T> : MonoSingleton<T> where T : CanvasSingleton<T>
{
    /// <summary> Whether the canvas is visible or hidden. </summary>
    public static bool Shown;

    /// <summary> Creates an instance of this singleton. </summary>
    public static void Build(string name, SafeEvent hideEvent = null, Action hideAction = null)
    {
        // initialize the singleton and create a canvas
        UI.Canvas(name, Plugin.Instance.transform).AddComponent<T>().gameObject.SetActive(hideEvent != null || hideAction != null);

        hideEvent ??= Events.OnLoaded;
        hideAction ??= () => Instance.gameObject.SetActive(Shown = false);

        // hide the interface once loading a level or main menu
        hideEvent += hideAction;
    }

    /// <summary> Creates a listener that creates an instance of this singleton on top of another one. </summary>
    public static void Build<L>() where L : MonoSingleton<L> =>
        Events.OnLoaded += () => MonoSingleton<L>.Instance.gameObject.AddComponent<T>();
}
