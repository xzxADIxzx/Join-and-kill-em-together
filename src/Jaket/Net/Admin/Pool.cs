namespace Jaket.Net.Admin;

/// <summary> Keeps a specific type of entities within the amount limit. </summary>
public struct Pool
{
    /// <summary> Size of the pool is limited: to add new ones, old ones must die. </summary>
    private Entity[] pool;
    /// <summary> Index of the next entity to kill after reaching the size limit. </summary>
    private int next;

    public Pool(int size) => pool = new Entity[size];

    /// <summary> Inserts the entity into any available slot if any; otherwise, frees a slot by killing an entity. </summary>
    public void Add(Entity entity)
    {
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i]?.Hidden ?? true)
            {
                pool[i] = entity;
                return;
            }
        }

        if (!pool[next]?.Hidden ?? false) pool[next].Kill();

        pool[next] = entity;

        next = ++next % pool.Length;
    }
}
