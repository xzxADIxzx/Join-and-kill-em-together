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
    public static bool Active;

    /// <summary> Whether the slowmo gamemode modifier is enabled. </summary>
    public static bool Slowmo;
    /// <summary> Whether respawn is locked due to gamemode logic. </summary>
    public static bool LockRespawn => Mode.HPs() && health[(byte)Networking.LocalPlayer.Team] <= 0;

    /// <summary> Number of health points given to each active team. </summary>
    private static int startHPs = 6;
    /// <summary> Number of health points each team currently has. </summary>
    private static int[] health = new int[Teams.All.Length];

    #region general

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            if (LobbyConfig.Mode != Mode.ToString().ToLower())
            {
                Mode = Gamemodes.All.Find(m => m.ToString().ToLower() == LobbyConfig.Mode);
                if (LobbyController.Online && LobbyController.IsOwner) LoadScn(Scene);
            }

            Slowmo = LobbyConfig.Slowmo;

            if (LobbyConfig.Hammer)
                Loadouts.Set(Loadouts.Make(true, l => l.altShotgun.greenVariant = VariantOption.ForceOn));
            else
                Loadouts.Set(null);

            if (LobbyController.Offline) UI.Chat.DisplayText(null, false);
        };
        Events.OnLoad += Countdown;
        Events.EveryHalf += () =>
        {
            if (LobbyController.Offline || !Active) return;
            if (Mode.HPs()) UpdateHPs();
        };
    }

    /// <summary> Counts a few seconds down before restarting the round. </summary>
    public static void Countdown()
    {
        if (Mode == Gamemode.Campaign)
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
        if (Mode.HPs())
        {
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

        UI.Spectator.Toggle();
        UI.Chat.DisplayText(null, false);
        Active = true;
    }

    /// <summary> Handles gamemode specific actions on player death. </summary>
    public static void OnDeath(Friend member)
    {
        if (Mode.HPs())
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

    private static void UpdateHPs()
    {
        int[] alive = new int[8];
        Networking.Entities.Player(p => p.Health > 0, p => alive[(byte)p.Team]++);
        if (nm.hp > 0)
            alive[(byte)Networking.LocalPlayer.Team]++;

        UI.Chat.DisplayText(string.Join("  ", Teams.All.Cast(t => health[(byte)t] > 0 || alive[(byte)t] > 0, t =>
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
