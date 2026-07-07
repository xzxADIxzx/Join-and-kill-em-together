namespace Jaket;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;

/// <summary> List of the project events and internal logic of the network ticks. </summary>
public class Events
{
    #region events

    /// <summary> Triggered after loading any scene. </summary>
    public static Runnable InternalSceneLoaded { set => UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => value(); }
    /// <summary> Triggered after finishing a flush. </summary>
    public static Runnable InternalFlushFinish = () => Log.Ready = true;

    /// <summary> Triggered when the loading of any scene starts. </summary>
    public static SafeEvent OnLoadingStart = new("load-start");
    /// <summary> Triggered when the loading of any scene ends. </summary>
    public static SafeEvent OnLoad = new("load-end");
    /// <summary> Triggered when the loading of the main menu ends. </summary>
    public static SafeEvent OnMainMenuLoad = new("main-menu-load");

    /// <summary> Triggered when an action is performed on a lobby. </summary>
    public static SafeEvent OnLobbyAction = new("lobby-action");
    /// <summary> Triggered when the local player enters the lobby. </summary>
    public static SafeEvent OnLobbyEnter = new("lobby-enter");

    /// <summary> Triggered when someone invites you to their lobby. </summary>
    public static SafeEvent<Lobby> OnLobbyInvite = new("lobby-invite");
    /// <summary> Triggered when someone joins the lobby. </summary>
    public static SafeEvent<Friend> OnMemberJoin = new("lobby-join");
    /// <summary> Triggered when someone leaves the lobby. </summary>
    public static SafeEvent<Friend> OnMemberLeave = new("lobby-leave");

    /// <summary> Triggered when a team composition changes. </summary>
    public static SafeEvent OnTeamChange = new("team-change");
    /// <summary> Triggered when a weapon or hand changes: weapon swap, hand color change. </summary>
    public static SafeEvent OnHandChange = new("hand-change");

    /// <summary> Triggered every subtick. </summary>
    public static SafeEvent EveryTick = new("tick");
    /// <summary> Triggered every half a second. </summary>
    public static SafeEvent EveryHalf = new("half");

    #endregion

    /// <summary> Subscribes to some internal events. </summary>
    public static void Load()
    {
        InternalSceneLoaded = () =>
        {
            OnLoad.Fire();
            if (Scene == "Main Menu") OnMainMenuLoad.Fire();
        };

        SteamMatchmaking.OnLobbyDataChanged += lobby => OnLobbyAction.Fire();

        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => OnLobbyInvite.Fire(lobby);

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) => OnMemberJoin.Fire(member);
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) => OnMemberLeave.Fire(member);

        OnLobbyAction += OnTeamChange.Fire;
        OnLobbyAction += OnHandChange.Fire;
        OnLobbyAction += () =>
        {
            Application.runInBackground = LobbyController.Online;
            DiscordController.Instance.FetchSceneActivity(Scene);
            SteamController.Instance.FetchSceneActivity(Scene);
        };
    }

    #region bridge

    /// <inheritdoc/>
    public static Bridge bridge = new();

    /// <summary> Posts the task for execution in the main thread. </summary>
    public static void Post(Runnable task) => bridge.Enqueue(task);
    /// <summary> Posts the task for execution in the main thread once the condition is met. </summary>
    public static void Post(Prov<bool> cond, Runnable task)
    {
        if (cond())
            task();
        else
            Post(() => Post(cond, task));
    }

    #endregion

    /// <summary> Safe event that will output all exceptions to the console and guarantee the execution of each listener, regardless of errors. </summary>
    public class SafeEvent<T>
    {
        /// <summary> Name of the event to display in logs. </summary>
        protected string Name;
        /// <summary> List of all event listeners. </summary>
        protected List<Cons<T>> listeners = new();

        /// <summary> Fires the event, ensuring that all listeners will be executed regardless of exceptions. </summary>
        public void Fire(T t)
        {
            int amount = listeners.Count;
            for (int i = 0; i < amount; i++)
            {
                try { listeners[i](t); }
                catch (Exception ex) { Log.Error($"[EVNT] Caught an exception in the {Name} event", ex); }
            }
        }

        /// <summary> Fires the event without arguments, ensuring that all listeners will be executed regardless of exceptions. </summary>
        public void Fire() => Fire(default);

        public SafeEvent(string name) => Name = name;

        public static SafeEvent<T> operator +(SafeEvent<T> e, Cons<T> listener) { e.listeners.Add(listener); return e; }
        public static SafeEvent<T> operator -(SafeEvent<T> e, Cons<T> listener) { e.listeners.Remove(listener); return e; }
    }

    /// <summary> Safe event that will output all exceptions to the console and guarantee the execution of each listener, regardless of errors. </summary>
    public class SafeEvent : SafeEvent<object>
    {
        public SafeEvent(string name) : base(name) { }

        public static SafeEvent operator +(SafeEvent e, Runnable listener) { _ = e + (_ => listener()); return e; }
        public static SafeEvent operator -(SafeEvent e, Runnable listener) { _ = e - (_ => listener()); return e; }
    }
}
