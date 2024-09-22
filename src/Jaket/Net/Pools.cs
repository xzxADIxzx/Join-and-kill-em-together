namespace Jaket.Net;

using System;

/// <summary> Simple hash map divided into four pools. Uses unsigned integers as keys and entities as values. </summary>
public class Pools
{
    /// <summary> Each fourth entry belongs to one pool. </summary>
    private Entry[] entries;

    public Pools() => entries = new Entry[1024];

    /// <summary> Adds a new entry if there is no entry with the given key, or changes the existing one. </summary>
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

    /// <summary> Returns whether an entity with the given key/id was found. </summary>
    public bool TryGet(uint key, out Entity value)
    {
        var entry = entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = entry.Next;
        value = entry?.Value;
        return entry != null;
    }

    /// <summary> Returns whether an entity with the given key/id is present in the hash map. </summary>
    public bool Contains(uint key) => TryGet(key, out _);

    /// <summary> Removes an entry with the given key/id. </summary>
    public void Remove(uint key)
    {
        ref var entry = ref entries[key & 0x3FF];

        while (entry != null && entry.Key != key) entry = ref entry.Next;
        entry = entry?.Next;
    }

    /// <summary> Removes all entries from the hash map. </summary>
    public void Clear() => Array.Clear(entries, 0, entries.Length);

    /// <summary> Iterates each entry in the hash map starting from the given value and with the given step. </summary>
    public void Each(int start, int step, Action<Entry> cons)
    {
        for (int i = start; i < entries.Length; i += step)
        {
            var entry = entries[i];
            while (entry != null)
            {
                cons(entry);
                entry = entry.Next;
            }
        }
    }

    /// <summary> Iterates each entry in the hash map. </summary>
    public void Each(Action<Entry> cons) => Each(0, 1, cons);

    /// <summary> Iterates each entry in the given pool. </summary>
    public void Each(int pool, Action<Entry> cons) => Each(pool, 4, cons);

    /// <summary> Counts the number of entries that are suitable for the given predicate. </summary>
    public int Count(Predicate<Entry> pred)
    {
        int amount = 0;
        Each(pair =>
        {
            if (pred(pair)) amount++;
        });
        return amount;
    }

    /// <summary> Counts the number of entries. </summary>
    public int Count() => Count(_ => true);

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
