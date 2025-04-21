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

    /// <summary> Identifier of the last lobby owner. </summary>
    public static uint LastOwner;
    /// <summary> Whether the player owns the lobby. </summary>
    public static bool IsOwner;

    /// <summary> Whether a lobby is being created at the moment. </summary>
    public static bool Creating;
    /// <summary> Whether lobbies are being fetched at the moment. </summary>
    public static bool Fetching;

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        // general info about the lobby
        Events.OnLobbyAction += () => Log.Debug($"[LOBY] Lobby owner is {Lobby?.Owner.ToString() ?? "null"}, level is {LobbyConfig.Level ?? "null"}");
        // get the owner id after entering a lobby
        Events.OnLobbyEnter += () =>
        {
            if (Offline) return;
            Log.Info($"[LOBY] Entered a lobby owned by {LastOwner = Lobby?.Owner.Id.AccountId ?? 0u}");

            if (LobbyConfig.Banned.Any(b => b == AccId.ToString()))
            {
                // notify the player to avoid confusion
                Bundle.Hud2NS("lobby.banned");

                LeaveLobby();
                Log.Info("[LOBY] Left the lobby as you are banned there");
            }
        };
        // and leave the lobby if the owner has left it
        Events.OnMemberLeave += m =>
        {
            if (LastOwner == m.Id.AccountId)
            {
                LeaveLobby();
                Log.Info("[LOBY] Left the lobby as the owner did the same");
            }
        };

        // put the name of the loaded level into the config to display in the lobby browser
        Events.OnLoad += () => LobbyConfig.Level = Scene;
        // loading the main menu is equivalent to leaving the lobby
        Events.OnMainMenuLoad += () => LeaveLobby(false);
    }

    /// <summary> Whether there is a player with the given id among the members of the lobby. </summary>
    public static bool Contains(uint id) => Lobby?.Members.Any(m => m.Id.AccountId == id) ?? false;

    #region control

    /// <summary> Creates a new lobby and connects to it. </summary>
    public static void CreateLobby()
    {
        if (Creating || Online) return;
        Log.Info("[LOBY] Creating a lobby...");

        Creating = true;
        SteamMatchmaking.CreateLobbyAsync(8).ContinueWith(t =>
        {
            Creating = false;
            Lobby = t.Result;

            Lobby?.SetJoinable(IsOwner = true);
            Lobby?.SetPrivate();
            LobbyConfig.Reset();

            Log.Info("[LOBY] Successfully created a lobby");
        });
    }

    /// <summary> Leaves the lobby. If the player is the owner, then all other players will be thrown to the main menu. </summary>
    public static void LeaveLobby(bool loadMainMenu = true)
    {
        if (Offline) return;
        Log.Info("[LOBY] Leaving the lobby...");

        Lobby?.Leave();
        Lobby = null;

        Networking.Close();
        Events.OnLobbyAction.Fire();

        // load the main menu if the player is a client
        if (!IsOwner && loadMainMenu) LoadScn("Main Menu");
    }

    /// <summary> Opens the overlay with an invitation dialog. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Connects the player to the given lobby. </summary>
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
                IsOwner = false;
                Lobby = lobby;
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
            cons(t.Result ?? new Lobby[0]);

            Log.Info($"[LOBY] Fetched {t.Result?.Length ?? 0} lobbies");
        });
    }

    #endregion
}
