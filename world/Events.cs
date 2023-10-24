namespace Jaket.World;

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> List of events used by the mod. Some of them are combined into one for simplicity. </summary>
public class Events
{
    /// <summary> Events triggered after loading any scene and the main menu. </summary>
    public static SafeEvent OnLoaded = new(), OnMainMenuLoaded = new();
    /// <summary> Event triggered when team composition changes. </summary>
    public static SafeEvent OnTeamChanged = new();

    /// <summary> Subscribes to some events to fire some safe events. </summary>
    public static void Load()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // any scene has loaded
            OnLoaded.Fire();

            // the main menu has loaded, this is much less often used, but it is used
            if (SceneHelper.CurrentScene == "Main Menu") OnMainMenuLoaded.Fire();
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) => OnTeamChanged.Fire();
    }
}

/// <summary> Safe event that will output all exceptions to the Unity console and guarantee the execution of each listener, regardless of errors. </summary>
public class SafeEvent
{
    /// <summary> List of all event listeners. </summary>
    private List<Action> listeners = new();

    /// <summary> Fires the event, i.e. fires its listeners, ensuring that they all will be executed regardless of exceptions. </summary>
    public void Fire() => listeners.ForEach(listener =>
    {
        try { listener(); }
        catch (Exception ex) { Debug.LogException(ex); }
    });

    /// <summary> Subscribes to the safe event: the listener can throw exceptions safely. </summary>
    public static SafeEvent operator +(SafeEvent e, Action listener) { e.listeners.Add(listener); return e; }

    /// <summary> Unsubscribes from the safe event if it finds the listener in the list. </summary>
    public static SafeEvent operator -(SafeEvent e, Action listener) { e.listeners.Remove(listener); return e; }
}
