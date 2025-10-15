namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Net.Types;
using Jaket.Net.Vendors;

/// <summary> Class responsible for entities, their vendors and identifiers. </summary>
public static class Entities
{
    /// <summary> Last used identifier, subsequent ones will be greater. </summary>
    private static uint? last;

    #region vendors

    public static Vendor Enemies;
    public static Items Items = new();
    public static Weapons Weapons = new();
    public static Projectiles Projectiles = new();
    public static Damage Damage = new();

    #endregion

    /// <summary> Loads all of the available vendors, thus, loading prefabs and suppliers as well. </summary>
    public static void Load()
    {
        Events.OnLobbyEnter += () => last = null;
        Events.OnMemberJoin += __ => last = null;

        Vendor.Suppliers[(byte)EntityType.Player] = (id, type) => new RemotePlayer(id, type);

        Vendor[] vendors = { Items, Weapons, Projectiles, Damage };
        vendors.Each(v => v.Load());

        Log.Info($"[ENTS] Loaded {vendors.Length} vendors");
    }

    /// <summary> Instantiates a new entity with the given identifier. </summary>
    public static Entity Supply(uint id, EntityType type) => Vendor.Suppliers[(int)type](id, type);

    /// <summary> Instantiates a new entity and generates an identifier for it. </summary>
    public static Entity Supply(EntityType type)
    {
        if (last == null)
        {
            uint factor = 0x20000000; // uint.MaxValue plus one divided by eight
            uint sector = AccId / factor;

            // the range of all possible identifiers is divided into eight equal sectors
            List<uint>[] sectors = [[], [], [], [], [], [], [], []];

            // lobby members are distributed among these sectors according to their identifiers
            LobbyController.Lobby?.Members.Each(m => sectors[m.Id.AccountId / factor].Add(m.Id.AccountId));

            // then we are given a number of steps equal to the number of members located in the same sector to the right of us
            int steps = sectors[sector].Count(m => m > AccId);

            // at each turn we move to the next sector and add the population of that sector to the number of steps
            while (steps > 0) steps += sectors[sector = (sector + 1) % 8].Count - 1;

            // this ensures that all of the lobby members are distributed across their sectors regardless of whom the algorithm is run for
            last = sector * factor;

            // a little extra information never hurts
            if (Version.DEBUG) Log.Debug($"[ENTS] Generated a new starting identifier, the sector is {sector}");
        }

        do last++;
        while (Networking.Entities.Contains(last.Value));

        return Supply(last.Value, type);
    }

    /// <summary> Vendors are responsible for their respective group of entity types. </summary>
    public interface Vendor
    {
        /// <summary> Prefabs of the objects manipulated by entities. </summary>
        public static GameObject[] Prefabs = new GameObject[byte.MaxValue + 1];
        /// <summary> Suppliers that provide the entities themselves. </summary>
        public static Supplier[] Suppliers = new Supplier[byte.MaxValue + 1];

        /// <summary> Returns the index of the prefab that is suitable for the given predicate in the given range. </summary>
        public static EntityType Find(EntityType from, EntityType to, Pred<GameObject> pred)
        {
            for (EntityType i = from; i <= to; i++)
            {
                if (pred(Prefabs[(byte)i])) return i;
            }
            return EntityType.None;
        }

        /// <summary> Loads prefabs of entities managed by the vendor. </summary>
        public abstract void Load();

        /// <summary> Resolves the type of the given object or returns none. </summary>
        public abstract EntityType Type(GameObject obj);

        /// <summary> Instantiates an object to manipulate via entity agent. </summary>
        public abstract GameObject Make(EntityType type, Vector3 position = default, Transform parent = null);

        /// <summary> Synchronizes the given object between network members. </summary>
        public abstract void Sync(GameObject obj, params bool[] args);
    }

    /// <summary> Instantiates a new entity of the given type. </summary>
    public delegate Entity Supplier(uint id, EntityType type);
}
