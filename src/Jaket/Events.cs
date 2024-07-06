namespace Jaket;

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Jaket.Net;

/// <summary> List of events used by the mod. Some of them are combined into one for simplicity. </summary>
public class Events : MonoSingleton<Events>
{
    /// <summary> Event triggered when a loading of any scene has started. </summary>
    public static SafeEvent OnLoadingStarted = new();
    /// <summary> Events triggered after loading any scene and the main menu. </summary>
    public static SafeEvent OnLoaded = new(), OnMainMenuLoaded = new();
    /// <summary> Event triggered when an action is taken on the lobby: creation, closing or connection. </summary>
    public static SafeEvent OnLobbyAction = new();
    /// <summary> Event triggered when the local player enters a lobby. </summary>
    public static SafeEvent OnLobbyEntered = new();
    /// <summary> Event triggered when a team composition changes. </summary>
    public static SafeEvent OnTeamChanged = new();
    /// <summary> Event triggered when a weapon or hand changes: weapon swap, hand color change. </summary>
    public static SafeEvent OnWeaponChanged = new();

    /// <summary> List of tasks that will need to be completed in the late update. </summary>
    public static Queue<Action> Tasks = new();
    /// <summary> Events that fire every net tick, second and dozen seconds. </summary>
    public static SafeEvent EveryTick = new(), EverySecond = new(), EveryDozen = new();

    /// <summary> Subscribes to some events to fire some safe events. </summary>
    public static void Load()
    {
        // initialize the singleton
        Tools.Create<Events>("Events");

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            OnLoaded.Fire();
            if (Tools.Scene == "Main Menu") OnMainMenuLoaded.Fire();
        };

        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) => Post(OnTeamChanged.Fire);
        SteamMatchmaking.OnLobbyDataChanged += lobby => OnLobbyAction.Fire();
        SteamMatchmaking.OnLobbyEntered += lobby => OnLobbyEntered.Fire();

        // interaction with the lobby affects many aspects of the game
        OnLobbyAction += OnTeamChanged.Fire;
        OnLobbyAction += OnWeaponChanged.Fire;
        OnLobbyAction += () =>
        {
            // update the discord & steam activity so everyone can know I've been working hard
            DiscordController.Instance.FetchSceneActivity(Tools.Scene);
            SteamController.Instance.FetchSceneActivity(Tools.Scene);

            // enable the ability of the game to run in the background, because multiplayer requires it
            Application.runInBackground = LobbyController.Online;
        };
    }

    /// <summary> Posts the task for execution in the late update. </summary>
    public static void Post(Action task) => Tasks.Enqueue(task);
    /// <summary> Posts the task for execution in the next frame. </summary>
    public static void Post2(Action task) => Post(() => Post(task));

    private void Dozen() => EveryDozen.Fire();
    private void Second() => EverySecond.Fire();
    private void Tick() => EveryTick.Fire();

    private void Start()
    {
        InvokeRepeating("Dozen", 1f, 12f);
        InvokeRepeating("Second", 1f, 1f);
        InvokeRepeating("Tick", 1f, Networking.SNAPSHOTS_SPACING);
    }

    private void LateUpdate()
    {
        int amount = Tasks.Count;
        for (int i = 0; i < amount; i++) Tasks.Dequeue()?.Invoke();
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
            catch (Exception ex) { Log.Error(ex); }
        }
    }

    /// <summary> Subscribes to the safe event: the listener can throw exceptions safely. </summary>
    public static SafeEvent operator +(SafeEvent e, Action listener) { e.listeners.Add(listener); return e; }

    /// <summary> Unsubscribes from the safe event if it finds the listener in the list. </summary>
    public static SafeEvent operator -(SafeEvent e, Action listener) { e.listeners.Remove(listener); return e; }
}
