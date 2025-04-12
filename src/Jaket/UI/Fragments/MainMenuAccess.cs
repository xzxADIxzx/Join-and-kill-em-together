namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Net;
using Jaket.UI.Dialogs;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides access to lobbies through the main menu. </summary>
public class MainMenuAccess : Fragment
{
    public MainMenuAccess(Transform root) : base(root, "MainMenuAccess", true, hide: UI.Access.Toggle)
    {
        Content.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var col = Random.value < .1f ? pink : green;
        var drk = Darker(col);

        Builder.Image(Rect("Line", new(645f, -255f, 570f, 3f)), null, col, ImageType.Simple);
        Builder.Image(Rect("Line", new(645f, -428f, 570f, 3f)), null, drk, ImageType.Simple);

        Builder.TextButton(Rect("Button", new(645f, -300f, 570f, 75f)), Tex.Large, col, "#lobby-tab.list", 36, TextAnchor.MiddleCenter, LobbyList.Instance.Toggle);
        Builder.TextButton(Rect("Button", new(645f, -384f, 570f, 75f)), Tex.Large, drk, "#lobby-tab.join", 36, TextAnchor.MiddleCenter, LobbyController.JoinByCode);

        Builder.Text(Rect("Tip", new(645f, -490f, 620f, 40f)), "#access", 21, white, TextAnchor.MiddleCenter);
    }
}
