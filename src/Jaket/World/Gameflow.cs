namespace Jaket.World;

using Steamworks;
using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.UI.Fragments;

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
    /// <summary> Whether the bleedy gamemode modifier is enabled. </summary>
    public static bool Bleedy { get; private set; }
    /// <summary> Whether respawn is locked due to gamemode logic. </summary>
    public static bool LockRespawn => Mode.HPs() | Mode.NoRestarts() && health[(byte)Networking.LocalPlayer.Team] <= 0;

    /// <summary> Number of health points each team currently has. </summary>
    private static byte[] health = new byte[Teams.All.Length];
    /// <summary> Identifiers of weapons each team has been given. </summary>
    private static byte[] weapon = new byte[Teams.All.Length];

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
                UI.Spectator.Rebuild();
                UI.Chat.DisplayText(null, false);
                Time.timeScale = 1f;
            }

            if (Hammer != LobbyConfig.Hammer) Loadouts.Set
            (
                (Hammer = LobbyConfig.Hammer) ? Loadouts.Make(true, l => l.altShotgun.greenVariant = VariantOption.ForceOn) : null
            );
            Slowmo = LobbyConfig.Slowmo;
            Bleedy = LobbyConfig.Bleedy;
        };
        Events.OnLoad += Countdown;
        Events.EveryHalf += () =>
        {
            if (LobbyController.Offline || !Active) return;
            if (Mode.HPs()) UpdateHPs();
            if (Mode.WTO()) UpdateWTO();
            if (Mode == Gamemode.Hardcore || Spectator.Special) UpdateRES();

            if (Bleedy) nm.GetHurt(1, Mode != Gamemode.Hardcore, 0f);
        };
    }

    /// <summary> Counts a few seconds down before restarting the round. </summary>
    public static void Countdown()
    {
        if (Mode == Gamemode.Campaign || Mode == Gamemode.BossRush || Mode == Gamemode.Hardcore)
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
        if (Mode.HPs() || Mode.NoRestarts())
        {
            Teams.All.Each(t => health[(byte)t] = byte.MaxValue);
            Count(out var sated, out _, out _);

            if (sated.Count(c => c) <= 1 && Mode != Gamemode.Hardcore)
            {
                Bundle.Ext("game.lone-team");
                return;
            }
            else if (!nm.dead) nm.GetHealth(100, true);
        }
        if (Mode.WTO() && LobbyController.IsOwner)
        {
            int data = 0;
            Teams.All.Each(t => data |= Random.Range(0, 24) << (byte)t * 5);
            LobbyController.Lobby?.SendChatString("#/s" + data);
        }

        UI.Spectator.Rebuild();
        UI.Chat.DisplayText(null, false);
        Active = true;
    }

    /// <summary> Handles gamemode specific actions on round start. </summary>
    public static void OnStart(uint data)
    {
        if (Mode.WTO())
        {
            Teams.All.Each(t => weapon[(byte)t] = (byte)( (int)data >> (byte)t * 5 & 0x1F ));

            Loadouts.Set(Loadouts.Make(false, weapon[(byte)Networking.LocalPlayer.Team]));
        }
    }

    /// <summary> Handles gamemode specific actions on player death. </summary>
    public static void OnDeath(Friend member)
    {
        if (Mode.HPs() || Mode.NoRestarts())
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
        if (Mode.WTO())
        {
            var team = Networking.GetTeam(member);
            var dead = Networking.Entities.Count(e => e is RemotePlayer p && p.Team == team && p.Health > 0) == 0;
            if (dead) Loadouts.Set(Loadouts.Merge
            (
                GunSetter.Instance.forcedLoadout,
                Loadouts.Make(false, weapon[(byte)team])
            ));
        }
        UI.Spectator.Rebuild();
    }

    /// <summary> Handles gamemode specific actions on team victory. </summary>
    public static void OnVictory(byte champ)
    {
        Bundle.Hud("game.win", false, $"#team.No{champ}", ColorUtility.ToHtmlStringRGBA(((Team)champ).Color()));
        Countdown();
    }

    #endregion
    #region specific

    private static void Count(out bool[] sated, out byte[] alive, out Team champ)
    {
        sated = new bool[Teams.All.Length];
        alive = new byte[Teams.All.Length];
        champ = Team.None;

        sated[(byte)Networking.LocalPlayer.Team] = true;
        var s = sated;
        Networking.Entities.Player(p => s[(byte)p.Team] = true);

        if (nm.hp > 0) alive[(byte)Networking.LocalPlayer.Team]++;
        var a = alive;
        Networking.Entities.Player(p => p.Health > 0, p => a[(byte)p.Team]++);

        Teams.All.Each
        (
            t => health[(byte)t] != byte.MaxValue && !s[(byte)t],
            t => health[(byte)t] = byte.MaxValue
        );

        Teams.All.Each
        (
            t => health[(byte)t] == byte.MaxValue && a[(byte)t] > 0,
            t => health[(byte)t] = (byte)(Mode.HPs() ? 6 : 1)
        );

        if (health.Count(h => h != byte.MaxValue) >= 2 && health.Count(h => h != byte.MaxValue && h > 0) <= 1 && alive.Count(a => a > 0) <= 1)
        {
            champ = Teams.All.Find(t => a[(byte)t] > 0);
        }
    }

    private static void UpdateHPs()
    {
        Count(out _, out var alive, out var champ);

        UI.Chat.DisplayText(string.Join("  ", Teams.All.Cast(t => health[(byte)t] != byte.MaxValue & health[(byte)t] > 0 || alive[(byte)t] > 0, t =>
        {
            var common = ColorUtility.ToHtmlStringRGBA(       t.Color() );
            var dimmed = ColorUtility.ToHtmlStringRGBA(Darker(t.Color()));
            var display = new string[6];

            for (int i = 0; i < 6; i++) display[i]
                = i < health[(byte)t]
                ? $"[#{common}]:heart:[]"
                : i <  alive[(byte)t]
                ? $"[#{dimmed}]:heart:[]"
                : " ";

            return string.Join("[8] []", display);
        })));

        if (LobbyController.IsOwner && champ != Team.None) LobbyController.Lobby?.SendChatString("#/v" + (byte)champ);
    }

    private static void UpdateWTO()
    {
        Count(out _, out _, out var champ);

        if (LobbyController.IsOwner && champ != Team.None) LobbyController.Lobby?.SendChatString("#/v" + (byte)champ);
    }

    private static void UpdateRES()
    {
        if (nm.hp > 0 || Networking.Entities.Count(e => e is RemotePlayer p && p.Health > 0) > 0) return;

        if (Scene == "Endless")
        {
            var rank = nm.GetComponentInChildren<FinalCyberRank>();
            if (rank.savedTime == 0f) rank.GameOver();
        }
        else if (LobbyController.IsOwner)
        {
            UI.Spectator.Shown = false;
            StatsManager.Instance.Restart();
        }
    }

    #endregion
}
