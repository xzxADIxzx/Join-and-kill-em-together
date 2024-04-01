namespace Jaket.UI.Fragments;

using UnityEngine;

using Jaket.Net;
using Jaket.UI.Dialogs;

/// <summary> Access to the mod functions through the main menu. </summary>
public class MainMenuAccess : CanvasSingleton<MainMenuAccess>
{
    /// <summary> Table containing the access buttons. </summary>
    private Transform table;
    /// <summary> Main menu table. </summary>
    private GameObject menu;

    private void Start()
    {
        table = UIB.Rect("Access Table", transform, new(0f, -364f, 720f, 40f));
        table.gameObject.AddComponent<HudOpenEffect>();

        UIB.Button("#lobby-tab.join", table, new(-182f, 0f, 356f, 40f), clicked: LobbyController.JoinByCode).targetGraphic.color = new(1f, .1f, .9f);
        UIB.Button("#lobby-tab.list", table, new(182f, 0f, 356f, 40f), clicked: LobbyList.Instance.Toggle).targetGraphic.color = new(1f, .4f, .8f);
    }

    private void Update() => table.gameObject.SetActive(menu.activeSelf);

    /// <summary> Toggles visibility of the access table. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = Tools.Scene == "Main Menu");
        if (Shown) (menu = Tools.ObjFind("Main Menu (1)")).transform.Find("Panel").transform.localPosition = new(0f, -292f, 0f);
    }
}
