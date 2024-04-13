namespace Jaket.Net;

using Steamworks;
using System.Collections.Generic;

using Jaket.Content;

/// <summary> Class dedicated to protecting the lobby from unfavorable people. </summary>
public class Administration
{
    /// <summary> Max amount of common bullets per second. </summary>
    public const int MAX_COMMON_BULLETS_PS = 16;
    /// <summary> Max amount of entity bullets per player. </summary>
    public const int MAX_ENTITY_BULLETS_PP = 8;
    /// <summary> Max amount of enemies per player. </summary>
    public const int MAX_ENEMIES_PP = 16;
    /// <summary> Max amount of plushies per player. </summary>
    public const int MAX_PLUSHIES_PP = 8;

    /// <summary> List of banned player ids. </summary>
    public static List<SteamId> Banned = new();

    public static Dictionary<ulong, int> CommonBullets = new();
    public static Tree EntityBullets = new();
    public static Tree Enemies = new();
    public static Tree Plushies = new();

    /// <summary> Subscribes to events to clear lists. </summary>
    public static void Load()
    {
        Events.OnLobbyEntered += () => { Banned.Clear(); EntityBullets.Clear(); Enemies.Clear(); Plushies.Clear(); };
        Events.EverySecond += CommonBullets.Clear;
    }

    /// <summary> Kicks the member from the lobby, or rather asks him to leave, because Valve has not added such functionality to its API. </summary>
    public static void Ban(SteamId id)
    {
        // who does the client think he is?!
        if (!LobbyController.IsOwner) return;

        Networking.Send(PacketType.Kick, null, (data, size) =>
        {
            var con = Networking.FindCon(id);
            con?.SendMessage(data, size);
            con?.Flush();
            con?.Close();
        });

        Banned.Add(id);
        LobbyController.Lobby?.SendChatString("#/k" + id);
        LobbyController.Lobby?.SetData("banned", string.Join(" ", Banned));
    }

    /// <summary> Whether the player can spawn another common bullet. </summary>
    public static bool CanSpawnCommonBullet(SteamId id, int amount) => Increase(id, amount) <= MAX_COMMON_BULLETS_PS;

    /// <summary> Whether the player can spawn another entity bullet. </summary>
    public static bool CanSpawnEntityBullet(SteamId id) => Count(id, EntityBullets) <= MAX_ENTITY_BULLETS_PP;

    /// <summary> Adds a new enemy to the list and kills the old one. </summary>
    public static void EnemySpawned(SteamId id, Entity entity, bool big)
    {
        // player can only spawn one big enemy at a time
        if (big && Enemies.TryGetValue(id, out var list)) list.ForEach(e => e.Kill());

        // kill an old enemy if the player has exceeded the limit
        if (Count(id, Enemies) >= MAX_ENEMIES_PP) Enemies[id][0].Kill();
        Enemies[id].Add(entity);
    }

    /// <summary> Adds a new plushy to the list and destroys the old one. </summary>
    public static void PlushySpawned(SteamId id, Entity entity)
    {
        if (Count(id, Plushies) >= MAX_PLUSHIES_PP)
        {
            Networking.Send(PacketType.KillEntity, w => w.Id(Plushies[id][0].Id), size: 8);
            Plushies[id][0].Kill();
        }
        Plushies[id].Add(entity);
    }

    #region tools

    /// <summary> Increases the counter of bullets sent by the player at that second. </summary>
    public static int Increase(SteamId id, int amount)
    {
        CommonBullets.TryGetValue(id, out int value);
        return CommonBullets[id] = value + amount;
    }

    /// <summary> Counts the number of living entities the given player has in the tree. </summary>
    public static int Count(SteamId id, Tree tree)
    {
        if (tree.ContainsKey(id))
            return tree[id].Count - tree[id].RemoveAll(entity => entity == null);
        else
        {
            tree[id] = new();
            return 0;
        }
    }

    #endregion

    /// <summary> Just a shortcut to avoid writing a lot of code. </summary>
    public class Tree : Dictionary<SteamId, List<Entity>> { }
}
