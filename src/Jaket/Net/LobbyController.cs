namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using UnityEngine;

using Jaket.Assets;

/// <summary> Class responsible for managing the lobby. </summary>
public static class LobbyController
{
    /// <summary> Matchmaking lobby that the player is connected to. </summary>
    public static Lobby? Lobby;
    public static bool Online => Lobby != null;
    public static bool Offline => Lobby == null;

    /// <summary> Identifier of the lobby owner. </summary>
    public static uint Owner;
    /// <summary> Whether the player owns the lobby. </summary>
    public static bool IsOwner;

    /// <summary> Whether a lobby is being produced. </summary>
    public static bool Creating { get; private set; }
    /// <summary> Whether lobbies are being fetched. </summary>
    public static bool Fetching { get; private set; }

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            Log.Debug($"[LOBY] Lobby name is {LobbyConfig.Name ?? "null"}, mode is {LobbyConfig.Mode ?? "null"}, level is {LobbyConfig.Level ?? "null"}");

            if (Online && !IsOwner && LobbyConfig.Banned.Has(AccId.ToString()))
            {
                // notify the player to avoid confusion
                Bundle.Hud2NS("lobby.banned");

                LeaveLobby();
                Log.Info("[LOBY] Left the lobby due to being banned");
            }

            if (Online && !IsOwner && Version.HasIncompatibility && !LobbyConfig.ModsAllowed)
            {
                // notify the player to avoid confusion
                Bundle.Hud2NS("lobby.modded");

                LeaveLobby();
                Log.Info("[LOBY] Left the lobby due to incompatible mods");
            }
        };

        Events.OnMemberLeave += m =>
        {
            if (Owner == m.AccId)
            {
                LeaveLobby();
                Log.Info("[LOBY] Left the lobby due to owner leaving");
            }
        };

        Events.OnLoad += () => LobbyConfig.Level = Scene;
        Events.OnMainMenuLoad += () => LeaveLobby(false);
    }

    /// <summary> Whether there is a player with the given id among the members of the lobby. </summary>
    public static bool Contains(uint id) => Lobby?.Members.Any(m => m.AccId == id) ?? false;

    #region control

    /// <summary> Creates a new lobby and joins it. </summary>
    public static void CreateLobby()
    {
        if (Creating || Online) return;
        Log.Info("[LOBY] Creating a lobby...");

        Creating = true;
        SteamMatchmaking.CreateLobbyAsync(8).ContinueWith(t =>
        {
            Creating = false;
            Lobby = t.Result;
            Owner = AccId;
            IsOwner = true;

            Lobby?.SetJoinable(true);
            Lobby?.SetPrivate();
            LobbyConfig.Reset();

            Events.OnLobbyEnter.Fire();
            Log.Info("[LOBY] Successfully created a lobby");
        });
    }

    /// <summary> Leaves the lobby and, if necessary, loads the main menu. </summary>
    public static void LeaveLobby(bool load = true)
    {
        if (Creating || Offline) return;
        Log.Info("[LOBY] Leaving the lobby...");

        Lobby?.Leave();
        Lobby = null;

        Networking.Close();
        if (load) LoadScn("Main Menu");

        Events.OnLobbyAction.Fire();
        Log.Info("[LOBY] Successfully left the lobby");
    }

    /// <summary> Opens an invitation dialog. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Connects to the given lobby. </summary>
    public static void JoinLobby(Lobby lobby)
    {
        if (Lobby?.Id == lobby.Id) return;
        Log.Info("[LOBY] Joining a lobby...");

        // leave the previous lobby before joining the new one
        if (Online) LeaveLobby(false);

        lobby.Join().ContinueWith(t =>
        {
            if (t.Result == RoomEnter.Success)
            {
                Lobby = lobby;
                Owner = lobby.Owner.AccId;
                IsOwner = false;

                Events.OnLobbyEnter.Fire();
                Log.Info($"[LOBY] Successfully joined the lobby");
            }
            else Log.Warning($"[LOBY] Couldn't join the lobby, the result is {t.Result}");
        });
    }

    #endregion
    #region codes & browser

    /// <summary> Copies the lobby code to the clipboard. </summary>
    public static void CopyCode()
    {
        GUIUtility.systemCopyBuffer = Lobby?.Id.ToString() ?? "How?";
        Bundle.Hud("lobby.copied");
    }

    /// <summary> Joins by the lobby code from the clipboard. </summary>
    public static void JoinByCode()
    {
        if (ulong.TryParse(GUIUtility.systemCopyBuffer, out var code)) JoinLobby(new(code));
        else Bundle.Hud("lobby.failed");
    }

    /// <summary> Fetches the list of public lobbies. </summary>
    public static void FetchLobbies(Cons<Lobby[]> cons)
    {
        if (Fetching) return;
        Log.Info("[LOBY] Fetching the list of public lobbies...");

        Fetching = true;
        SteamMatchmaking.LobbyList.WithKeyValue("client", "jaket").RequestAsync().ContinueWith(t =>
        {
            Fetching = false;
            cons(t.Result ?? []);

            Log.Info($"[LOBY] Fetched {t.Result?.Length ?? 0} lobbies");
        });
    }

    #endregion
}
