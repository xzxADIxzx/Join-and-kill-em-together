namespace Jaket.UI.Fragments;

using Jaket.Net;
using Jaket.UI.Dialogs;

/// <summary> Access to the mod functions through the main menu. </summary>
public class MainMenuAccess : CanvasSingleton<MainMenuAccess>
{
    private void Start()
    {
        UIB.Button("#lobby-tab.join", transform, new(-182f, -364f, 356f, 40f), clicked: LobbyController.JoinByCode).targetGraphic.color = new(1f, .1f, .9f);
        UIB.Button("#lobby-tab.list", transform, new(182f, -364f, 356f, 40f), clicked: LobbyList.Instance.Toggle).targetGraphic.color = new(1f, .4f, .8f);
    }

    /// <summary> Toggles visibility of the access table. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = Tools.Scene == "Main Menu");
        if (gameObject.activeSelf) Tools.ObjFind("Main Menu (1)").transform.Find("Panel").transform.localPosition = new(0f, -292f, 0f);
    }
}
