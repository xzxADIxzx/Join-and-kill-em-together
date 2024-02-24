namespace Jaket.UI;

using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Net;
using Jaket.World;

/// <summary> Small interactive guide for new players. </summary>
public class InteractiveGuide : CanvasSingleton<InteractiveGuide>
{
    /// <summary> Offer to help the player. </summary>
    public const string OFFER = "Press <color=orange>F11</color> to launch Jaket's interactive guide";
    /// <summary> Guide on how to skip the guide. </summary>
    public const string SKIP = "<color=#cccccccc>Press <color=orange>Esc</color> to skip</color>";

    /// <summary> List with the conditions for completing each of the stages of the guide. </summary>
    public List<Func<bool>> Conditions = new();

    /// <summary> Whether the player was asked to complete the guide. </summary>
    private bool offered;
    /// <summary> Index of the current part of the guide. </summary>
    private int index;

    private void Start()
    {
        Add($"Hi! Press {Settings.LobbyTab} to open the lobby tab", 0f, 0f, () => LobbyTab.Shown);
        Add("Create a lobby using the button on the left", -267f, 436f, () => LobbyController.Lobby != null);
        Add("Now invite a friend and wait for him to connect", -239f, 372f, () => LobbyController.Lobby?.MemberCount > 1);
        Add($"Press {Settings.LobbyTab} to close the lobby tab", 0f, 0f, () => !LobbyTab.Shown);

        Add($"Open chat by pressing {Settings.Chat}", 0f, 0f, () => Chat.Shown);
        Add("And send a message to your friend", 0f, -444f, () => !Chat.Shown);

        Add($"Open settings using {Settings.Settingz} key", 0f, 0f, () => Settings.Shown);
        Add("Now you are ready to play, good luck :D", 0f, 0f, () => !Settings.Shown);
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
    public void Add(string text, float x, float y, Func<bool> completed)
    {
        float width = Chat.RawMessageLength(text) * 14f + 16f;
        UI.Table("Guide " + Conditions.Count, transform, x, y, width, 60f, table =>
        {
            UI.Text(text, table, 0f, 10f, width, size: 24);
            UI.Text(SKIP, table, 0f, -14f, width, size: 16);
        }).gameObject.SetActive(false);

        Conditions.Add(completed);
    }

    /// <summary> Offers the player to go through the guide or closes it if the main menu has been loaded. </summary>
    public void OfferAssistance()
    {
        if (Tools.Scene == "Main Menu")
        {
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            index = 0; Shown = false;
        }
        else if (!offered) UI.SendMsg(OFFER, offered = true);
    }

    /// <summary> Launches the guide. </summary>
    public void Launch()
    {
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        transform.GetChild(index = 0).gameObject.SetActive(Shown = true);
        Movement.UpdateState();
    }
}
