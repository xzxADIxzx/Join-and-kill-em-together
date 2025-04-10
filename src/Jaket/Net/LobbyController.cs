namespace Jaket.Net;

using Steamworks;
using Steamworks.Data;
using System.Linq;
using UnityEngine;

using Jaket.Assets;
using Jaket.IO;

/// <summary> Lobby controller with several useful methods and properties. </summary>
public class LobbyController
{
    /// <summary> The current lobby the player is connected to. Null if the player is not connected to any lobby. </summary>
    public static Lobby? Lobby;
    public static bool Online => Lobby != null;
    public static bool Offline => Lobby == null;

    /// <summary> Id of the last lobby owner, needed to track the exit of the host and for other minor things. </summary>
    public static SteamId LastOwner;
    /// <summary> Whether the player owns the lobby. </summary>
    public static bool IsOwner;

    /// <summary> Whether a lobby is creating right now. </summary>
    public static bool CreatingLobby;
    /// <summary> Whether a list of public lobbies is being fetched right now. </summary>
    public static bool FetchingLobbies;

    /// <summary> Whether PvP is allowed in this lobby. </summary>
    public static bool PvPAllowed => Lobby?.GetData("pvp") == "True";
    /// <summary> Whether cheats are allowed in this lobby. </summary>
    public static bool CheatsAllowed => Lobby?.GetData("cheats") == "True";
    /// <summary> Whether mods are allowed in this lobby. </summary>
    public static bool ModsAllowed => Lobby?.GetData("mods") == "True";
    /// <summary> Whether bosses must be healed after death in this lobby. </summary>
    public static bool HealBosses => Lobby?.GetData("heal-bosses") == "True";
    /// <summary> Number of percentages that will be added to the boss's health for each player. </summary>
    public static float PPP;

    /// <summary> Scales health to increase difficulty. </summary>
    public static void ScaleHealth(ref float health) => health *= 1f + Mathf.Min(Lobby?.MemberCount - 1 ?? 1, 1) * PPP;
    /// <summary> Whether the given lobby is created via Multikill. </summary>
    public static bool IsMultikillLobby(Lobby lobby) => lobby.Data.Any(pair => pair.Key == "mk_lobby");

    /// <summary> Creates the necessary listeners for proper work. </summary>
    public static void Load()
    {
        // general info about the lobby
        Events.OnLobbyAction += () => Log.Debug($"Lobby updated: owner is {Lobby?.Owner.ToString() ?? "null"}, level is {Lobby?.GetData("level") ?? "null"}");
        // get the owner id when entering the lobby
        Events.OnLobbyEnter += () =>
        {
            if (Offline) return;
            Log.Debug($"Entered a lobby ({LastOwner = Lobby?.Owner.Id ?? 0L})");

            if (Lobby.Value.GetData("banned").Contains(AccId.ToString()))
            {
                LeaveLobby();
                Bundle.Hud2NS("lobby.banned");
            }
            if (IsMultikillLobby(Lobby.Value))
            {
                LeaveLobby();
                Bundle.Hud2NS("lobby.mk");
            }
        };
        // and leave the lobby if the owner has left it
        Events.OnMemberLeave += member =>
        {
            if (member.Id == LastOwner) LeaveLobby();
        };

        // put the level name in the lobby data so that it can be seen in the public lobbies list
        Events.OnLoad += () => Lobby?.SetData("level", MapMap(Scene));
        // if the player exits to the main menu, then this is equivalent to leaving the lobby
        Events.OnMainMenuLoad += () => LeaveLobby(false);
    }

    /// <summary> Is there a user with the given id among the members of the lobby. </summary>
    public static bool Contains(uint id) => Lobby?.Members.Any(member => member.Id.AccountId == id) ?? false;

    /// <summary> Returns the member at the given index or null. </summary>
    public static Friend? At(int index) => Lobby?.Members.ElementAt(Mathf.Min(Mathf.Max(index, 0), Lobby.Value.MemberCount));

    /// <summary> Returns the index of the local player in the lits of members. </summary>
    public static int IndexOfLocal() => Lobby?.Members.ToList().FindIndex(member => member.IsMe) ?? 0;

    #region control

    /// <summary> Asynchronously creates a new lobby with default settings and connects to it. </summary>
    public static void CreateLobby()
    {
        if (Lobby != null || CreatingLobby) return;
        Log.Debug("Creating a lobby...");

        CreatingLobby = true;
        SteamMatchmaking.CreateLobbyAsync(8).ContinueWith(task =>
        {
            CreatingLobby = false; IsOwner = true;
            Lobby = task.Result;

            Lobby?.SetJoinable(true);
            Lobby?.SetPrivate();
            Lobby?.SetData("jaket", "true");
            Lobby?.SetData("name", $"{SteamClient.Name}'s Lobby");
            Lobby?.SetData("level", MapMap(Scene));
            Lobby?.SetData("pvp", "True");
            Lobby?.SetData("cheats", "False");
            Lobby?.SetData("mods", "False");
            Lobby?.SetData("heal-bosses", "True");
        });
    }

    /// <summary> Leaves the lobby. If the player is the owner, then all other players will be thrown into the main menu. </summary>
    public static void LeaveLobby(bool loadMainMenu = true)
    {
        Log.Debug("Leaving the lobby...");

        if (Online) // free up resources allocated for packets that have not been sent
        {
            Networking.Server.Close();
            Networking.Client.Close();
            Networking.Clear();
            Pointers.Free();

            Lobby?.Leave();
            Lobby = null;
        }

        // load the main menu if the client has left the lobby
        if (!IsOwner && loadMainMenu) LoadScn("Main Menu");

        Events.OnLobbyAction.Fire();
    }

    /// <summary> Opens Steam overlay with a selection of a friend to invite to the lobby. </summary>
    public static void InviteFriend() => SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);

    /// <summary> Asynchronously connects the player to the given lobby. </summary>
    public static void JoinLobby(Lobby lobby)
    {
        if (Lobby?.Id == lobby.Id) { Bundle.Hud("lobby.join-yourself"); return; }
        Log.Debug("Joining a lobby...");

        // leave the previous lobby before join the new, but don't load the main menu
        if (Online) LeaveLobby(false);

        lobby.Join().ContinueWith(task =>
        {
            if (task.Result == RoomEnter.Success)
            {
                IsOwner = false;
                Lobby = lobby;
            }
            else Log.Warning($"Couldn't join the lobby. Result is {task.Result}");
        });
    }

    #endregion
    #region codes

    /// <summary> Copies the lobby code to the clipboard. </summary>
    public static void CopyCode()
    {
        GUIUtility.systemCopyBuffer = Lobby?.Id.ToString();
        if (Online) Bundle.Hud("lobby.copied");
    }

    /// <summary> Joins by the lobby code from the clipboard. </summary>
    public static void JoinByCode()
    {
        if (ulong.TryParse(GUIUtility.systemCopyBuffer, out var code)) JoinLobby(new(code));
        else Bundle.Hud("lobby.failed");
    }

    #endregion
    #region browser

    /// <summary> Asynchronously fetches a list of public lobbies. </summary>
    public static void FetchLobbies(Cons<Lobby[]> done)
    {
        FetchingLobbies = true;
        SteamMatchmaking.LobbyList.RequestAsync().ContinueWith(task =>
        {
            FetchingLobbies = false;
            done(task.Result.Where(l => l.Data.Any(p => p.Key == "jaket" || p.Key == "mk_lobby")).ToArray());
        });
    }

    /// <summary> Maps the map name so that it is more understandable to an average player. </summary>
    public static string MapMap(string map) => map switch
    {
        "Tutorial" => "Tutorial",
        "uk_construct" => "Sandbox",
        "Endless" => "Cyber Grind",
        "CreditsMuseum2" => "Museum",
        "Intermission1" => "Intermission",
        "Intermission2" => "Intermission",
        _ => map.Substring("Level ".Length)
    };

    #endregion
}
