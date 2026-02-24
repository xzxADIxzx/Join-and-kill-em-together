namespace Jaket.World;

using Steamworks;
using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;

using static Jaket.UI.Lib.Pal;

/// <summary> Class responsible for managing the flow of the game. </summary>
public class Gameflow
{
    static NewMovement nm => NewMovement.Instance;

    /// <summary> Actual gamemode controling the flow of the game. </summary>
    public static Gamemode Mode { get; private set; }
    /// <summary> Whether the extant round is in the active state. </summary>
    public static bool Active { get; private set; }

    /// <summary> Whether the slowmo gamemode modifier is enabled. </summary>
    public static bool Slowmo { get; private set; }
    /// <summary> Whether the hammer gamemode modifier is enabled. </summary>
    public static bool Hammer { get; private set; }

    /// <summary> Whether respawn is locked due to gamemode logic. </summary>
    public static bool LockRespawn => Mode.HPs() | Mode.NoRestarts() && health[(byte)Networking.LocalPlayer.Team] <= 0;

    /// <summary> Number of health points given to each active team. </summary>
    private static int startHPs = 6;
    /// <summary> Number of health points each team currently has. </summary>
    private static int[] health = new int[Teams.All.Length];
    /// <summary> Identifiers of weapons each team has been given. </summary>
    private static int[] weapon = new int[Teams.All.Length];

    #region general

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            if (LobbyConfig.Mode != Mode.ToString().ToLower())
            {
                Mode = Gamemodes.All.Find(m => m.ToString().ToLower() == LobbyConfig.Mode);
                Active = false;
                if (LobbyController.Online && LobbyController.IsOwner) LoadScn(Scene);
            }

            if (LobbyController.Offline)
            {
                UI.Spectator.Toggle();
                UI.Chat.DisplayText(null, false);
                Time.timeScale = 1f;
            }

            if (Hammer != LobbyConfig.Hammer) Loadouts.Set
            (
                (Hammer = LobbyConfig.Hammer) ? Loadouts.Make(true, l => l.altShotgun.greenVariant = VariantOption.ForceOn) : null
            );
            Slowmo = LobbyConfig.Slowmo;
        };
        Events.OnLoad += Countdown;
        Events.EveryHalf += () =>
        {
            if (LobbyController.Offline || !Active) return;
            if (Mode.HPs()) UpdateHPs();
            if (Mode == Gamemode.ArmsRace) UpdateHPs(false); // TODO arms race should prob have its own update loop 
        };
    }

    /// <summary> Counts a few seconds down before restarting the round. </summary>
    public static void Countdown()
    {
        if (Mode == Gamemode.Campaign || Mode == Gamemode.BossRush)
        {
            Restart();
            return;
        }
        else Active = false;

        static IEnumerator Countdown(int seconds)
        {
            while (seconds > 0)
            {
                Bundle.Ext("game.countdown", $"{seconds--}");
                yield return new WaitForSeconds(1);
            }
            Restart();
        }
        Plugin.Instance.StartCoroutine(Countdown(8));
    }

    /// <summary> Restarts the round, doesn't do anything in campaign. </summary>
    public static void Restart()
    {
        if (Mode.PvP()) LobbyConfig.PvPAllowed = true;
        if (Mode.HPs() | Mode.NoRestarts())
        {
            startHPs = Mode.HPs() ? 6 : 1;

            health.Clear();
            Teams.All.Each
            (
                t => Networking.LocalPlayer.Team == t || Networking.Entities.Count(e => e is RemotePlayer p && p.Team == t) > 0,
                t => health[(byte)t] = startHPs
            );
            if (health.Count(h => h != 0) <= 1)
            {
                Bundle.Ext("game.lone-team");
                return;
            }
        }
        if (Mode == Gamemode.ArmsRace && LobbyController.IsOwner)
        {
            int data = 0;
            Teams.All.Each(t => data |= Random.Range(0, 24) << (byte)t * 5);
            LobbyController.Lobby?.SendChatString("#/s" + data);
        }

        UI.Spectator.Toggle();
        UI.Chat.DisplayText(null, false);
        Active = true;
    }

    /// <summary> Handles gamemode specific actions on round start. </summary>
    public static void OnStart(uint data)
    {
        if (Mode == Gamemode.ArmsRace)
        {
            Teams.All.Each(t => weapon[(byte)t] = (int)data >> (byte)t * 5 & 0x1F);

            Loadouts.Set(Loadouts.Make(false, (byte)weapon[(byte)Networking.LocalPlayer.Team]));
        }
    }

    /// <summary> Handles gamemode specific actions on player death. </summary>
    public static void OnDeath(Friend member)
    {
        if (Mode.HPs() | Mode.NoRestarts())
        {
            if (member.IsMe)
                health[(byte)(Networking.LocalPlayer.Team)]--;
            else
                health[(byte)(Networking.Entities[member.AccId] as RemotePlayer).Team]--;
        }
        if (Mode.HealOnKill())
        {
            int fraction = LobbyController.Lobby?.MemberCount - 1 ?? 1;
            nm.ForceAddAntiHP(-100 / fraction);
            nm.GetHealth(100 / fraction, true);
        }
        if (Mode == Gamemode.ArmsRace)
        {
            var team = Networking.GetTeam(member);
            var dead = Networking.Entities.Count(e => e is RemotePlayer p && p.Team == team && p.Health > 0) == 0;
            if (dead) Loadouts.Set(Loadouts.Merge
            (
                GunSetter.Instance.forcedLoadout,
                Loadouts.Make(false, (byte)weapon[(byte)team])
            ));
        }
        // update the info label
        UI.Spectator.Toggle();
    }

    /// <summary> Handles gamemode specific actions on team victory. </summary>
    public static void OnVictory(byte winner)
    {
        Bundle.Hud("game.win", false, $"#team.No{winner}", ColorUtility.ToHtmlStringRGBA(((Team)winner).Color()));
        Countdown();
    }

    #endregion
    #region specific

    private static void UpdateHPs(bool display = true)
    {
        int[] alive = new int[8];
        Networking.Entities.Player(p => p.Health > 0, p => alive[(byte)p.Team]++);
        if (nm.hp > 0)
            alive[(byte)Networking.LocalPlayer.Team]++;

        if (display) UI.Chat.DisplayText(string.Join("  ", Teams.All.Cast(t => health[(byte)t] > 0 || alive[(byte)t] > 0, t =>
        {
            var common = ColorUtility.ToHtmlStringRGBA(       t.Color() );
            var dimmed = ColorUtility.ToHtmlStringRGBA(Darker(t.Color()));
            var display = new string[startHPs];

            for (int i = 0; i < startHPs; i++) display[i]
                = i < health[(byte)t]
                ? $"[#{common}]:heart:[]"
                : i < alive [(byte)t]
                ? $"[#{dimmed}]:heart:[]"
                : " ";

            return string.Join("[8] []", display);
        })));

        if (LobbyController.IsOwner && health.Count(h => h > 0) <= 1 && alive.Count(a => a > 0) <= 1)
        {
            var winner = Teams.All.Find(t => alive[(byte)t] > 0);
            LobbyController.Lobby?.SendChatString("#/w" + (byte)winner);
        }
    }

    #endregion
}
