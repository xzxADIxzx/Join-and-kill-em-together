namespace Jaket.Net;

using Player = Types.RemotePlayer;

/// <summary> Simple hash map divided into four pools. Uses unsigned integers as keys and entities as values. </summary>
public class Pools
{
    /// <summary> Each fourth entry belongs to the same pool. </summary>
    private Entry[] entries = new Entry[1024];

    #region general

    /// <summary> Adds a new entry if there is no entry with the given key/id, or changes the existing one. </summary>
    public void Set(uint key, Entity value)
    {
        ref var entry = ref entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = ref entry.Next;
        entry ??= new() { Key = key };
        entry.Value = value;
    }

    /// <summary> Returns an entity with the given key/id if any, or null. </summary>
    public Entity Get(uint key)
    {
        var entry = entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = entry.Next;
        return entry?.Value;
    }

    /// <summary> Returns whether an entity with the given key/id was found in the hash map. </summary>
    public bool TryGetValue(uint key, out Entity value)
    {
        var entry = entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = entry.Next;
        value = entry?.Value;
        return entry != null;
    }

    /// <summary> Returns whether an entity with the given key/id is present in the hash map. </summary>
    public bool Contains(uint key) => TryGetValue(key, out _);

    /// <summary> Removes an entity with the given key/id from the hash map. </summary>
    public void Remove(uint key)
    {
        ref var entry = ref entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = ref entry.Next;
        entry = entry?.Next;
    }

    /// <summary> Removes all entities from the hash map. </summary>
    public void Clear() => entries.Clear();

    #endregion
    #region iteration

    /// <summary> Iterates each entity in the hash map starting from the given value and with the specified step. </summary>
    public void Each(int start, int step, Cons<Entity> cons)
    {
        for (int i = start; i < entries.Length; i += step)
        {
            var entry = entries[i];
            while (entry != null)
            {
                cons(entry.Value);
                entry = entry.Next;
            }
        }
    }

    /// <summary> Iterates each nonnull entity in the hash map. </summary>
    public void Each(Cons<Entity> cons)                      => Each(0, 1, cons);
    /// <summary> Iterates each nonnull entity in the hash map. </summary>
    public void Each(Pred<Entity> pred, Cons<Entity> cons)   => Each(0, 1, e => { if (pred(e)) cons(e); });

    /// <summary> Iterates each visible entity in the hash map. </summary>
    public void Alive<T>(Cons<T> cons)                       => Each(0, 1, e => { if (!e.Hidden && e is T t) cons(t); });
    /// <summary> Iterates each visible entity in the hash map. </summary>
    public void Alive<T>(Pred<T> pred, Cons<T> cons)         => Each(0, 1, e => { if (!e.Hidden && e is T t && pred(t)) cons(t); });

    /// <summary> Iterates each visible player in the hash map. </summary>
    public void Player(Cons<Player> cons)                    => Each(0, 1, e => { if (!e.Hidden && e is Player p) cons(p); });
    /// <summary> Iterates each visible player in the hash map. </summary>
    public void Player(Pred<Player> pred, Cons<Player> cons) => Each(0, 1, e => { if (!e.Hidden && e is Player p && pred(p)) cons(p); });

    /// <summary> Iterates each visible entity in the given server pool. </summary>
    public void ServerPool(int i, Cons<Entity> cons)         => Each(i, 4, e => { if (!e.Hidden) cons(e); });
    /// <summary> Iterates each visible entity in the given client pool. </summary>
    public void ClientPool(int i, Cons<Entity> cons)         => Each(i, 4, e => { if (!e.Hidden && e.IsOwner) cons(e); });

    /// <summary> Counts the number of entities that are suitable for the given predicate. </summary>
    public int Count(Pred<Entity> pred)
    {
        int amount = 0;
        Each(pred, _ => amount++);
        return amount;
    }

    /// <summary> Counts the number of entities. </summary>
    public int Count() => Count(_ => true);

    #endregion

    /// <summary> Simple way to modify the hash map. </summary>
    public Entity this[uint key]
    {
        set => Set(key, value);
        get => Get(key);
    }

    /// <summary> Hash map entry. Next can be null. </summary>
    public class Entry
    {
        public uint Key;
        public Entity Value;
        public Entry Next;
    }
}
