namespace Jaket.UI;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Utils
{
    public static bool WasCheatsEnabled;

    #region general

    public static void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;

        // lock camera
        CameraController.Instance.enabled = !enable;
    }

    public static void ToggleMovement(bool enable)
    {
        NewMovement.Instance.enabled = enable;
        NewMovement.Instance.GetComponentInChildren<GunControl>().enabled = enable;
        NewMovement.Instance.GetComponentInChildren<FistControl>().enabled = enable;

        // temporary disable cheats
        if (enable)
            CheatsController.Instance.cheatsEnabled = WasCheatsEnabled;
        else
        {
            WasCheatsEnabled = CheatsController.Instance.cheatsEnabled;
            CheatsController.Instance.cheatsEnabled = false;
        }
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
    #region text

    public static GameObject Text(string name, Transform parent, float x, float y, float width, float height, int size, Color color, TextAnchor align)
    {
        var obj = Rect(name, parent, x, y, width, height);
        Component<Text>(obj, text =>
        {
            text.text = name;
            text.font = OptionsMenuToManager.Instance.optionsMenu.transform.Find("Text").gameObject.GetComponent<Text>().font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
        });

        return obj;
    }

    public static GameObject Text(string name, Transform parent, float x, float y)
    {
        return Text(name, parent, x, y, 320f, 64f, 36, Color.white, TextAnchor.MiddleCenter);
    }

    #endregion
    #region button

    public static GameObject Button(string name, Transform parent, float x, float y, float width, float height, int size, Color color, TextAnchor align, UnityAction clicked)
    {
        var obj = Image(name, parent, x, y, width, height, Color.white);
        Component<Button>(obj, button =>
        {
            button.targetGraphic = obj.GetComponent<Image>();
            button.colors = OptionsMenuToManager.Instance.pauseMenu.transform.Find("Continue").gameObject.GetComponent<Button>().colors;
            button.onClick.AddListener(clicked);
        });

        // add text to the button
        Text(name, obj.transform, 0f, 0f, width, height, size, color, align);

        return obj;
    }

    public static GameObject Button(string name, Transform parent, float x, float y, UnityAction clicked)
    {
        return Button(name, parent, x, y, 320f, 64f, 36, Color.white, TextAnchor.MiddleCenter, clicked);
    }

    public static void SetText(GameObject obj, string text)
    {
        obj.GetComponentInChildren<Text>().text = text;
    }

    public static void SetInteractable(GameObject obj, bool interactable)
    {
        obj.GetComponent<Button>().interactable = interactable;
    }

    #endregion
    #region field

    public static InputField Field(string name, Transform parent, float x, float y, float width, float height, int size, UnityAction<string> enter)
    {
        var obj = Image(name, parent, x, y, width, height, new Color(0f, 0f, 0f, .5f));

        var text = Text("", obj.transform, 8f, -1f, width, height, size, Color.white, TextAnchor.MiddleLeft);
        var placeholder = Text(name, obj.transform, 8f, -1f, width, height, size, new Color(.8f, .8f, .8f, .8f), TextAnchor.MiddleLeft);

        Component<InputField>(obj, field =>
        {
            field.targetGraphic = obj.GetComponent<Image>();
            field.textComponent = text.GetComponent<Text>();
            field.placeholder = placeholder.GetComponent<Text>();

            field.onEndEdit.AddListener(enter);
        });

        return obj.GetComponent<InputField>();
    }

    #endregion
}
