namespace Jaket.World;

using Steamworks;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using UnityEngine;

using static Jaket.UI.Lib.Pal;

/// <summary> Class responsible for managing the flow of the game. </summary>
public class Gameflow
{
    static NewMovement nm => NewMovement.Instance;

    /// <summary> Actual gamemode controling the flow of the game. </summary>
    public static Gamemode Mode { get; private set; }

    private static int startHPs = 6;
    private static int[] health = new int[Teams.All.Length];

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            if (LobbyConfig.Mode != Mode.ToString().ToLower())
            {
                Mode = Gamemodes.All.Find(m => m.ToString().ToLower() == LobbyConfig.Mode);
                Restart();
            }
        };
        Events.OnLoad += Restart;
        Events.EveryHalf += () =>
        {
            if (Mode.HPs()) UpdateHPs();
        };
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
        }
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
            nm.GetHealth(100 / fraction, true);
        }
    }

    #region specific

    private static void UpdateHPs()
    {
        int[] alive = new int[8];
        Networking.Entities.Player(p => p.Health > 0, p => alive[(byte)p.Team]++);
        if (nm.hp > 0)
            alive[(byte)Networking.LocalPlayer.Team]++;

        UI.Chat.DisplayText(string.Join("  ", Teams.All.Cast(t => health[(byte)t] != 0 || alive[(byte)t] != 0, t =>
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
    }

    #endregion
}
