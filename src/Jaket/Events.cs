namespace Jaket;

using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Net;

/// <summary> List of project events, an important part of the internal logic. </summary>
public class Events : MonoBehaviour
{
    /// <summary> Internal event triggered after loading any scene. </summary>
    public static Runnable InternalSceneLoaded { set => UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => value(); }
    /// <summary> Internal event triggered after finishing any flush operation. </summary>
    public static Runnable InternalFlushFinish = () => { };

    #region events

    /// <summary> Triggered when a loading of any scene has started. </summary>
    public static SafeEvent OnLoadingStart = new("loading");
    /// <summary> Triggered when a loading of any scene has finished. </summary>
    public static SafeEvent OnLoad = new("load");
    /// <summary> Triggered when a loading of the main menu has finished. </summary>
    public static SafeEvent OnMainMenuLoad = new("main-menu-load");

    /// <summary> Triggered when an action is performed on a lobby: creation, closure or modification. </summary>
    public static SafeEvent OnLobbyAction = new("lobby-action");
    /// <summary> Triggered when the local player enters a lobby. </summary>
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

    #endregion

    /// <summary> List of tasks that will be completed on the next frame. </summary>
    public static Queue<Runnable> Tasks = new();
    /// <summary> Events that are fired every subtick, second and dozen of seconds. </summary>
    public static SafeEvent EveryTick = new("tick"), EverySecond = new("second"), EveryDozen = new("dozen");

    /// <summary> Subscribes to some internal events. </summary>
    public static void Load()
    {
        Create<Events>("Events");

        InternalSceneLoaded = () =>
        {
            OnLoad.Fire();
            if (Scene == "Main Menu") OnMainMenuLoad.Fire();
        };

        SteamMatchmaking.OnLobbyDataChanged += lobby => Post(OnLobbyAction.Fire);
        SteamMatchmaking.OnLobbyEntered += lobby => Post(OnLobbyEnter.Fire);

        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => OnLobbyInvite.Fire(lobby);

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) => OnMemberJoin.Fire(member);
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) => OnMemberLeave.Fire(member);

        OnLobbyAction += OnTeamChange.Fire;
        OnLobbyAction += OnHandChange.Fire;
        OnLobbyAction += () =>
        {
            // update the discord & steam activity so everyone can know I've been working hard
            DiscordController.Instance.FetchSceneActivity(Scene);
            SteamController.Instance.FetchSceneActivity(Scene);

            // enable the ability of the game to run in the background, because multiplayer requires it
            Application.runInBackground = LobbyController.Online;
        };
    }

    /// <summary> Posts the task for execution in the late update. </summary>
    public static void Post(Runnable task) => Tasks.Enqueue(task);
    /// <summary> Posts the task for execution in the next frame. </summary>
    public static void Post2(Runnable task) => Post(() => Post(task));

    private void Tick() => EveryTick.Fire();
    private void Second() => EverySecond.Fire();
    private void Dozen() => EveryDozen.Fire();

    private void Start()
    {
        InvokeRepeating("Tick", 1f, 1f / Networking.TICKS_PER_SECOND / Networking.SUBTICKS_PER_TICK);
        InvokeRepeating("Second", 1f, 1f);
        InvokeRepeating("Dozen", 1f, 12f);
    }

    private void LateUpdate()
    {
        int amount = Tasks.Count;
        for (int i = 0; i < amount; i++) Tasks.Dequeue()?.Invoke();
    }

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
