namespace Jaket.World;

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net;

/// <summary> List of events used by the mod. Some of them are combined into one for simplicity. </summary>
public class Events
{
    /// <summary> Events triggered after loading any scene and the main menu. </summary>
    public static SafeEvent OnLoaded = new(), OnMainMenuLoaded = new();
    /// <summary> Event triggered when an action is taken on the lobby: creation, closing or connection. </summary>
    public static SafeEvent OnLobbyAction = new();
    /// <summary> Event triggered when team composition changes. </summary>
    public static SafeEvent OnTeamChanged = new();
    /// <summary> Event triggered when a weapon or hand changes: weapon swap, hand color change. </summary>
    public static SafeEvent OnWeaponChanged = new();

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
        SteamMatchmaking.OnLobbyDataChanged += lobby => OnLobbyAction.Fire();

        // interaction with the lobby affects many aspects of the game
        OnLobbyAction += OnTeamChanged.Fire;
        OnLobbyAction += OnWeaponChanged.Fire;

        // update the discord activity so everyone can know I've been working hard
        OnLobbyAction += () => DiscordController.Instance.FetchSceneActivity(SceneHelper.CurrentScene);
        // toggle the ability of the game to run in the background, because multiplayer requires it
        OnLobbyAction += () => Application.runInBackground = LobbyController.Lobby != null;
    }
}

/// <summary> Safe event that will output all exceptions to the Unity console and guarantee the execution of each listener, regardless of errors. </summary>
public class SafeEvent
{
    /// <summary> List of all event listeners. </summary>
    private List<Action> listeners = new();

    /// <summary> Fires the event, i.e. fires its listeners, ensuring that they all will be executed regardless of exceptions. </summary>
    public void Fire()
    {
        for (int i = 0; i < listeners.Count; i++)
        {
            try { listeners[i](); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    /// <summary> Subscribes to the safe event: the listener can throw exceptions safely. </summary>
    public static SafeEvent operator +(SafeEvent e, Action listener) { e.listeners.Add(listener); return e; }

    /// <summary> Unsubscribes from the safe event if it finds the listener in the list. </summary>
    public static SafeEvent operator -(SafeEvent e, Action listener) { e.listeners.Remove(listener); return e; }
}
