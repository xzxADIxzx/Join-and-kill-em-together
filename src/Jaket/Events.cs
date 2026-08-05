namespace Jaket;

using Steamworks;
using Steamworks.Data;
using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;

/// <summary> List of the project events and internal logic of the network loop. </summary>
public static class Events
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

    /// <summary> Subscribes to several events for proper work. </summary>
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

        new Thread(Loop)
        {
            Name = "network",
            IsBackground = true
        }
        .Start();
    }

    #region thread

    /// <summary> Runs the network loop. </summary>
    public static void Loop()
    {
        long tick = Stopwatch.GetTimestamp();
        long half = Stopwatch.GetTimestamp();

        long step = Stopwatch.Frequency / Networking.TICKS_PER_SECOND / Networking.SUBTICKS_PER_TICK;
        long hsec = Stopwatch.Frequency / 2L;
        long spin = Stopwatch.Frequency / 1000L * 2L;

        while (true)
        {
            Stats.Jitter += Math.Abs
            (
                Stopwatch.GetTimestamp() - tick
            );
            Stats.Measure(ref Stats.Thread, EveryTick.Fire);

            if (Stopwatch.GetTimestamp() - half > hsec)
            {
                half = Stopwatch.GetTimestamp();
                Stats.Measure(ref Stats.Thread, EveryHalf.Fire);
            }

            if (Stopwatch.GetTimestamp() - tick > step)
            {
                Stats.Jitter += // abandon missed ticks
                Stopwatch.GetTimestamp() - tick - step;
                tick = Stopwatch.GetTimestamp() + step;
            }
            else tick += step;

            long delta;
            do
            {
                delta = tick - Stopwatch.GetTimestamp();

                if (delta > spin)
                    Thread.Sleep(1);
                else
                    Thread.SpinWait(64);
            }
            while (delta > 0L);
        }
    }

    #endregion
    #region bridge

    /// <inheritdoc cref="Bridge"/>
    private static Bridge bridge = new();

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

    /// <summary> Dequeues and executes all tasks, should only be called in the main thread. </summary>
    public static void Main() => bridge.Dequeue();

    #endregion

    /// <summary> Guarantees the execution of all listeners regardless of errors. </summary>
    public class SafeEvent<T>(string name)
    {
        protected string name = name;
        protected byte amount;
        protected Cons<T>[] listeners = new Cons<T>[byte.MaxValue + 1];

        public void Fire(T t)
        {
            for (byte i = 0; i < amount; i++)
            {
                try
                {
                    listeners[i](t);
                }
                catch (Exception e) { Log.Error($"[EVNT] Caught an exception in the {name} event", e); }
            }
        }

        public static SafeEvent<T> operator +(SafeEvent<T> e, Cons<T> listener) { e.listeners[e.amount++] = listener; return e; }
    }

    /// <inheritdoc/>
    public class SafeEvent(string name) : SafeEvent<object>(name)
    {
        public void Fire() => Fire(null);

        public static SafeEvent operator +(SafeEvent e, Runnable listener) { e.listeners[e.amount++] = _ => listener(); return e; }
    }
}
