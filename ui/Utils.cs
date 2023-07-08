namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

public class Utils
{
    #region general

    public static void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;

        // lock camera
        CameraController.Instance.enabled = !enable;
    }

    public static void Component<T>(GameObject obj, UnityAction<T> action) where T : Component
    {
        action.Invoke(obj.AddComponent<T>());
    }

    #endregion
    #region base

    public static GameObject Object(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);

        return obj;
    }

    public static GameObject Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var obj = Object(name, parent);
        Component<RectTransform>(obj, rect =>
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        });

        return obj;
    }

    #endregion
