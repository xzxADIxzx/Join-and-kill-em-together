namespace Jaket.Net;

using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Jaket.Content;
using Jaket.UI.Dialogs;

/// <summary> Class dedicated to protecting the lobby from unfavorable people. </summary>
public class Administration
{
    /// <summary> Max amount of bytes a player can send per second. </summary>
    public const int SPAM_RATE = 32 * 1024;
    /// <summary> Max amount of warnings a player can get before ban. </summary>
    public const int MAX_WARNINGS = 4;

    /// <summary> Max amount of entity bullets per player and common bullets per second. </summary>
    public const int MAX_BULLETS = 10;
    /// <summary> Max amount of entities per player. </summary>
    public const int MAX_ENTITIES = 16;
    /// <summary> Max amount of plushies per player. </summary>
    public const int MAX_PLUSHIES = 6;

    /// <summary> List of banned player ids. </summary>
    public static List<uint> Banned = new();
    public static List<uint> Kicked = new();
    /// <summary> List of banned player sprays. </summary>
    public static List<uint> BannedSprays = new();

    private static Counter spam = new();
    private static Counter warnings = new();
    private static Counter commonBullets = new();
    private static Tree entityBullets = new();
    private static Tree entities = new();
    private static Tree plushies = new();

    /// <summary> Subscribes to events to clear lists. </summary>
    public static void Load()
    {
        Events.OnLobbyAction += () =>
        {
            if (LobbyController.IsOwner) return;

            Banned.Clear();
            Kicked.Clear();
            LobbyController.Lobby?.GetData("banned").Split(' ').Do(sid =>
            {
                if (uint.TryParse(sid, out var id)) Banned.Add(id);
            });

            LobbyController.Lobby?.GetData("kicked").Split(' ').Do(sid =>
            {
                if (uint.TryParse(sid, out var id)) Kicked.Add(id);
            });
        };
        Events.OnLobbyEntered += () => { Banned.Clear(); Kicked.Clear(); entityBullets.Clear(); entities.Clear(); plushies.Clear(); };
        Events.EverySecond += spam.Clear;
        Events.EverySecond += commonBullets.Clear;
        Events.EveryDozen += warnings.Clear;
    }

    /// <summary> Kicks the member from the lobby, or rather asks him to leave, because Valve hasn't added such functionality to their API. </summary>
    public static void Ban(uint id)
    {

        Networking.Send(PacketType.Ban, null, (data, size) =>
        {
            var con = Networking.FindCon(id);
            Tools.Send(con, data, size);
            con?.Flush();
            Events.Post2(() => con?.Close());
        });

        Banned.Add(id);
        LobbyController.Lobby?.SendChatString("#/k" + id);
        LobbyController.Lobby?.SetData("banned", string.Join(" ", Banned));
    }

    public static void Kick(uint id)
    {
        // use dummy packet type to prevent jaket from telling people they were banned
        Networking.Send(PacketType.Dummy, null, (data, size) =>
        {
            var con = Networking.FindCon(id);
            Tools.Send(con, data, size);
            con?.Flush();
            Events.Post2(() => con?.Close());
        });

        Kicked.Add(id);
        LobbyController.Lobby?.SendChatString($"<color=red><size=14>Kicked {Tools.ChatStr(Tools.Name(id))}</color></size>");
        LobbyController.Lobby?.SetData("kicked", string.Join(" ", Kicked));
    }

    public static string BlacklistAdd(string name) {
        string ret = "";
        
        Networking.EachPlayer(player => {
            if (player.Header.Name == name)
            {
                if (!File.ReadAllLines(Plugin.UIDBlacklistPath).Contains(player.Header.Name))
                {
                    File.AppendAllText(Plugin.UIDBlacklistPath, player.Header.Name + "\n");
                    File.AppendAllText(Plugin.UIDBlacklistPath, player.Header.Id.ToString() + "\n");
                }

                if (LobbyController.IsOwner) Kick(player.Header.Id);
                ret = "[Blacklist] Blacklisted user " + player.Header.Id.ToString() + " :: \"" + Tools.ChatStr(player.Header.Name) + "\"";
            }
        });

        if (ret != "") {
            Tools.CacheBlacklist();
            return ret;
        };

        return "[Blacklist] \"" + Tools.ChatStr(name) + "\" is not a valid user";
    }

    public static string BlacklistAddUID(string uid) {
        string ret = "";
        
        Networking.EachPlayer(player => {
            if (player.Header.Id.ToString() == uid)
            {
                if (!File.ReadAllLines(Plugin.UIDBlacklistPath).Contains(player.Header.Name))
                {
                    File.AppendAllText(Plugin.UIDBlacklistPath, player.Header.Name + "\n");
                    File.AppendAllText(Plugin.UIDBlacklistPath, player.Header.Id.ToString() + "\n");
                }

                if (LobbyController.IsOwner) Kick(player.Header.Id);
                ret = "[Blacklist] Blacklisted user " + player.Header.Id.ToString() + " :: \"" + player.Header.Name + "\"";
            }
        });

        if (ret != "") {
            Tools.CacheBlacklist();
            return ret;
        };

        return $"[Blacklist] \"{uid}\" is not a valid user's uid";
    }

    public static string BlacklistRemove(string name) {
        bool valid = false;
        int i;

        for (i = 0; i < Tools.CachedBlacklist.Length; ++i)
        {
            if (uint.TryParse(Tools.CachedBlacklist[i], out uint uid)) 
            {
                if (Tools.CachedBlacklist[i - 1] == name) {
                    valid = true;
                    break;
                }
            }
        }

        if (!valid) return "[Blacklist] Invalid Name: \"" + Tools.ChatStr(name) + "\" This is your blacklist: \n" + BlacklistList();
        File.WriteAllLines(Plugin.UIDBlacklistPath, File.ReadLines(Plugin.UIDBlacklistPath).Where(l => l != Tools.CachedBlacklist[i - 1]).ToList());
        File.WriteAllLines(Plugin.UIDBlacklistPath, File.ReadLines(Plugin.UIDBlacklistPath).Where(l => l != Tools.CachedBlacklist[i]).ToList());
        Tools.CacheBlacklist();
        return $"[Blacklist] Removed user \"{name}\" from blacklist.";
    }

    public static string BlacklistList() {
        string ret = "";

        if (Tools.CachedBlacklist.Length == 0) {
            return "[Blacklist] There's nobody in your blacklist";
        }

        for (int i = 0; i < Tools.CachedBlacklist.Length; ++i) {
            string line = Tools.CachedBlacklist[i];

            if (uint.TryParse(line, out uint uid))
            {
                ret += $"[Blacklist] \"{Tools.CachedBlacklist[i - 1]}\"\n";
            }
        }

        return ret;
    }

    /// <summary> Whether the player is sending a large amount of data. </summary>
    public static bool IsSpam(uint id, int amount) => spam.Count(id, amount) >= SPAM_RATE;

    /// <summary> Clears the amount of data sent by the given player. </summary>
    public static void ClearSpam(uint id) => spam[id] = int.MinValue;

    /// <summary> Whether the player is trying to spam. </summary>
    public static bool IsWarned(uint id) => warnings.Count(id, 1) >= MAX_WARNINGS;

    /// <summary> Whether the player can spawn another common bullet. </summary>
    public static bool CanSpawnBullet(uint owner, int amount) => commonBullets.Count(owner, amount) <= MAX_BULLETS;

    /// <summary> Handles the creations of a new entity by a client. If the client exceeds its limit, the old entity will be destroyed. </ Summary>
    public static void Handle(uint owner, Entity entity)
    {
        void Default(Tree tree, int max)
        {
            if (tree.Count(owner) > max) tree[owner][0].NetKill();
            tree[owner].Add(entity);
        }

        if (entity.Type.IsEnemy() || entity.Type.IsItem())
        {
            // player can only spawn one big enemy at a time
            if (entity.Type.IsBigEnemy() && entities.TryGetValue(owner, out var list)) list.ForEach(e => e.NetKill());

            Default(entities, MAX_ENTITIES);
        }
        else if (entity.Type.IsPlushie()) Default(plushies, MAX_PLUSHIES);
        else if (entity.Type.IsBullet()) Default(entityBullets, MAX_BULLETS);
    }

    /// <summary> Counter of abstract actions done by players. </summary>
    public class Counter : Dictionary<uint, int>
    {
        /// <summary> Counts the number of actions done by the given player and increases it by some value. </summary>
        public new int Count(uint id, int amount)
        {
            TryGetValue(id, out int value);
            return this[id] = value + amount;
        }
    }

    /// <summary> Tree with players ids as roots and entities created by these players as children. </summary>
    public class Tree : Dictionary<uint, List<Entity>>
    {
        /// <summary> Counts the number of living entities the given player has in the tree. </summary>
        public new int Count(uint id)
        {
            if (ContainsKey(id))
                return this[id].Count - this[id].RemoveAll(entity => entity == null || entity.Dead);
            else
            {
                this[id] = new();
                return 0;
            }
        }
    }
}
