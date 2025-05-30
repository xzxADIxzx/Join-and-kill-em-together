namespace Jaket.UI.Fragments;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Assets;
using Jaket.Input;
using Jaket.Net;
using Jaket.UI.Dialogs;

/// <summary> Small interactive guide for new players. </summary>
public class InteractiveGuide : CanvasSingleton<InteractiveGuide>
{
    /// <summary> List with the conditions for completing each of the stages of the guide. </summary>
    public List<Prov<bool>> Conditions = new();

    /// <summary> Whether the player was asked to complete the guide. </summary>
    private bool offered;
    /// <summary> Index of the current part of the guide. </summary>
    private int index;

    private void Start()
    {
        Add(0f, 0f, () => UI.LobbyTab.Shown, Keybind.LobbyTab);
        Add(-256f, 456f, () => LobbyController.Online);
        Add(-256f, 408f, () => LobbyController.Lobby?.MemberCount > 1);
        Add(0f, 0f, () => !UI.LobbyTab.Shown, Keybind.LobbyTab);

        Add(0f, 0f, () => Chat.Shown, Keybind.Chat);
        Add(0f, -444f, () => !Chat.Shown);

        Add(0f, 0f, () => UI.Settings.Shown, Keybind.Settings);
        Add(-256f, -252f, () => false);
        Add(0f, 0f, () => !UI.Settings.Shown);
    }

    private void Update()
    {
        if (Shown && index < Conditions.Count && (Conditions[index]() || Input.GetKeyDown(KeyCode.Escape)))
        {
            transform.GetChild(index++).gameObject.SetActive(false);
            if (index < Conditions.Count) transform.GetChild(index).gameObject.SetActive(true);
            else
            {
                Shown = false;
                Movement.UpdateState();
            }
        }
    }

    /// <summary> Adds a new part to the guide. </summary>
    public void Add(float x, float y, Prov<bool> completed, Keybind arg = null)
    {
        var guide = Bundle.Format("guide." + Conditions.Count, arg?.FormatValue());
        var width = Bundle.CutColors(guide).Length * 14f + 16f;

        UIB.Table("Guide " + Conditions.Count, transform, new(x, y, width, 60f), table =>
        {
            UIB.Text(guide, table, new(0f, 10f, width, 60f));
            UIB.Text("#guide.skip", table, new(0f, -14f, width, 60f), size: 16);
        }).gameObject.SetActive(false);

        Conditions.Add(completed);
    }

    /// <summary> Offers the player to go through the guide or closes it if the main menu has been loaded. </summary>
    public void OfferAssistance()
    {
        if (Scene == "Main Menu")
        {
            transform.Each(c => c.gameObject.SetActive(false));
            index = 0; Shown = false;
        }
        else if (!offered) Bundle.Hud("guide.offer", offered = true);
    }

    /// <summary> Launches the guide. </summary>
    public void Launch()
    {
        transform.Each(c => c.gameObject.SetActive(false));
        transform.GetChild(index = 0).gameObject.SetActive(Shown = true);
        Movement.UpdateState();
    }
}
