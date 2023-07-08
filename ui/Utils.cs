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

    public static GameObject Image(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        var obj = Rect(name, parent, x, y, width, height);
        Component<Image>(obj, image =>
        {
            image.sprite = OptionsMenuToManager.Instance.pauseMenu.transform.Find("Continue").gameObject.GetComponent<Image>().sprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = color;
        });

        return obj;
    }

    public static GameObject Canvas(string name, Transform parent)
    {
        var obj = Object(name, parent);
        Component<Canvas>(obj, canvas =>
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // move to the top (ultraimportant)
        });
        Component<CanvasScaler>(obj, scaler =>
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        });
        obj.AddComponent<GraphicRaycaster>();

        return obj;
    }

    #endregion
    #endregion
