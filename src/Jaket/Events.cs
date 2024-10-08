namespace Jaket;

using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

using Jaket.Net;

/// <summary> List of events used by the mod. Some of them are combined into one for simplicity. </summary>
public class Events : MonoSingleton<Events>
{
    /// <summary> Internal event triggered after loading any scene. </summary>
    public static Action InternalSceneLoaded { set => UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => value(); }

    #region events

    /// <summary> Event triggered when a loading of any scene has started. </summary>
    public static SafeEvent OnLoadingStarted = new();
    /// <summary> Event triggered after loading any scene. </summary>
    public static SafeEvent OnLoaded = new();
    /// <summary> Event triggered after loading the main menu. </summary>
    public static SafeEvent OnMainMenuLoaded = new();

    /// <summary> Event triggered when an action is taken on the lobby: creation, closing, connection or modifying. </summary>
    public static SafeEvent OnLobbyAction = new();
    /// <summary> Event triggered when the local player enters a lobby. </summary>
    public static SafeEvent OnLobbyEntered = new();

    /// <summary> Event triggered when someone invites you to their lobby. </summary>
    public static SafeEvent<Lobby> OnLobbyInvite = new();
    /// <summary> Event triggered when someone joins the lobby. </summary>
    public static SafeEvent<Friend> OnMemberJoin = new();
    /// <summary> Event triggered when someone leaves the lobby. </summary>
    public static SafeEvent<Friend> OnMemberLeave = new();

    /// <summary> Event triggered when a team composition changes. </summary>
    public static SafeEvent OnTeamChanged = new();
    /// <summary> Event triggered when a weapon or hand changes: weapon swap, hand color change. </summary>
    public static SafeEvent OnWeaponChanged = new();

    #endregion

    /// <summary> List of tasks that will need to be completed in the late update. </summary>
    public static Queue<Action> Tasks = new();
    /// <summary> Events that fire every net tick, second and dozen seconds. </summary>
    public static SafeEvent EveryTick = new(), EverySecond = new(), EveryDozen = new();

    /// <summary> Subscribes to some events to fire some safe events. </summary>
    public static void Load()
    {
        Create<Events>("Events");

        InternalSceneLoaded = () =>
        {
            OnLoaded.Fire();
            if (Scene == "Main Menu") OnMainMenuLoaded.Fire();
        };

        SteamMatchmaking.OnLobbyDataChanged += lobby => OnLobbyAction.Fire();
        SteamMatchmaking.OnLobbyEntered += lobby => OnLobbyEntered.Fire();

        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => OnLobbyInvite.Fire(lobby);

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) => OnMemberJoin.Fire(member);
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) => OnMemberLeave.Fire(member);

        // interaction with the lobby affects many aspects of the game
        OnLobbyAction += OnTeamChanged.Fire;
        OnLobbyAction += OnWeaponChanged.Fire;
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

    /// <summary> Safe event that will output all exceptions to the console and guarantee the execution of each listener, regardless of errors. </summary>
    public class SafeEvent<T>
    {
        /// <summary> List of all event listeners. </summary>
        protected List<Cons<T>> listeners = new();

        /// <summary> Fires the event, ensuring that all listeners will be executed regardless of exceptions. </summary>
        public void Fire(T t)
        {
            for (int i = 0; i < listeners.Count; i++)
            {
                try { listeners[i](t); }
                catch (Exception ex) { Log.Error(ex); }
            }
        }

        /// <summary> Fires the event without arguments, ensuring that all listeners will be executed regardless of exceptions. </summary>
        public void Fire() => Fire(default);

        public static SafeEvent<T> operator +(SafeEvent<T> e, Cons<T> listener) { e.listeners.Add(listener); return e; }
        public static SafeEvent<T> operator -(SafeEvent<T> e, Cons<T> listener) { e.listeners.Remove(listener); return e; }
    }

    /// <summary> Safe event that will output all exceptions to the console and guarantee the execution of each listener, regardless of errors. </summary>
    public class SafeEvent : SafeEvent<object>
    {
        public static SafeEvent operator +(SafeEvent e, Action listener) { _ = e + (_ => listener()); return e; }
        public static SafeEvent operator -(SafeEvent e, Action listener) { _ = e - (_ => listener()); return e; }
    }
}
