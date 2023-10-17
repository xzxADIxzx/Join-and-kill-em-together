namespace Jaket.UI;

using System;
using UnityEngine.SceneManagement;

/// <summary> Singleton based on canvas. Used for interface construction. </summary>
public class CanvasSingleton<T> : MonoSingleton<T> where T : CanvasSingleton<T>
{
    /// <summary> Whether the canvas is visible or hidden. </summary>
    public static bool Shown;

    /// <summary> Creates an instance of this singleton. </summary>
    public static void Build(string name, bool hideInMainMenuOnly = false, Action onLoad = null)
    {
        // initialize the singleton and create a canvas
        UI.Canvas(name, Plugin.Instance.transform).AddComponent<T>().gameObject.SetActive(hideInMainMenuOnly || onLoad != null);

        // hide the interface once loading a level or main menu
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // player indicators should only be hidden when loading into the main menu
            if (!hideInMainMenuOnly || SceneHelper.CurrentScene == "Main Menu")
            {
                // the chat doesn't need to be hidden entirely, just the input field
                if (onLoad != null) onLoad();
                else Instance.gameObject.SetActive(Shown = false);
            }
        };
    }
}
