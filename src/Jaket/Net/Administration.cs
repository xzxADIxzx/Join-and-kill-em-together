namespace Jaket.Net;

using System.Collections.Generic;

using Jaket.Content;

/// <summary> Class dedicated to protecting the lobby from unfavorable people. </summary>
public class Administration
{
    /// <summary> Max amount of entity bullets per player and common bullets per second. </summary>
    public const int MAX_BULLETS = 12;
    /// <summary> Max amount of entities per player. </summary>
    public const int MAX_ENTITIES = 16;
    /// <summary> Max amount of plushies per player. </summary>
    public const int MAX_PLUSHIES = 6;

    /// <summary> List of banned player ids. </summary>
    public static List<uint> Banned = new();
    /// <summary> List of banned player sprays. </summary>
    public static List<uint> BannedSprays = new();

    private static Dictionary<uint, int> commonBullets = new();
    private static Tree entityBullets = new();
    private static Tree entities = new();
    private static Tree plushies = new();

    /// <summary> Subscribes to events to clear lists. </summary>
    public static void Load()
    {
        Events.OnLobbyEntered += () => { Banned.Clear(); entityBullets.Clear(); entities.Clear(); plushies.Clear(); };
        Events.EverySecond += commonBullets.Clear;
    }

    /// <summary> Kicks the member from the lobby, or rather asks him to leave, because Valve hasn't added such functionality to their API. </summary>
    public static void Ban(uint id)
    {
        // who does the client think he is?!
        if (!LobbyController.IsOwner) return;

        Networking.Send(PacketType.Ban, null, (data, size) =>
        {
            var con = Networking.FindCon(id);
            Tools.Send(con, data, size);
            con?.Flush();
            con?.Close();
        });

        Banned.Add(id);
        LobbyController.Lobby?.SendChatString("#/k" + id);
        LobbyController.Lobby?.SetData("banned", string.Join(" ", Banned));
    }

    /// <summary> Whether the player can spawn another common bullet. </summary>
    public static bool CanSpawnBullet(uint owner, int amount)
    {
        commonBullets.TryGetValue(owner, out int value);
        return (commonBullets[owner] = value + amount) <= MAX_BULLETS;
    }

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
        else if (entity.Type.IsPlushy()) Default(plushies, MAX_PLUSHIES);
        else if (entity.Type.IsBullet()) Default(entityBullets, MAX_BULLETS);
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
