namespace Jaket.Net.Admin;

using Jaket.Content;

/// <summary> Keeps track of various actions of a specific member. </summary>
public class Subject
{
    /// <summary> Identifier of the member. </summary>
    public readonly uint Id;
    /// <summary> Privileges of the member. </summary>
    public Privilege Privilege;

    /// <summary> These prevent flood of actions. </summary>
    private Ratekeeper

    warns   = new(3f, .02f),
    packets = new(Networking.TICKS_PER_SECOND * 120f, Networking.TICKS_PER_SECOND * 80f),
    hitscns = new(64f, 16f),
    commons = new(12f, 8f);

    /// <summary> These prevent flood of entities. </summary>
    private Pool

    defpool = new(24),
    project = new(24),
    flashes = new(32),
    harpoon = new(4);

    public Subject(uint id) => Id = id;

    /// <summary> Gives the member a warning if the given limit is exceeded. </summary>
    public void Warn(string reason, ref Ratekeeper limit)
    {
        if (limit.Kept()) return;
        if (warns.Kept())
        {
            limit.Reset();
            Log.Warning($"[ADMIN] {Id} was warned: {reason}");
        }
        else Ban("out of warnings");
    }

    /// <summary> Bans the member and, if provided, kills the given entity. </summary>
    public void Ban(string reason, Entity entity = null)
    {
        Administration.Ban(Id);
        Log.Warning($"[ADMIN] {Id} was blocked: {reason}");

        // prevent naughty script kitties from spawning bosses/prisons
        entity?.Kill();
    }

    /// <summary> Handles reception of a packet. </summary>
    public void Handle() => Warn("flood of packets", ref packets);

    /// <summary> Handles creation of a hitscan. </summary>
    public void Handle(EntityType type)
    {
        if (!Privilege.Has) Warn("flood of hitscns", ref hitscns);
    }

    /// <summary> Handles creation of an entity. </summary>
    public void Handle(Entity entity)
    {
        var type = entity.Type;
        if (type.IsEnemy())
        {
            if (Privilege.Has)
            {
                defpool.Add(entity);
                Warn("flood of entities", ref commons);
            }
            else Ban("abuse of entities", entity);
        }
        else if (type.IsItem())
        {
            if (Privilege.Has || type.IsFish() || type.IsPlushie() || type == EntityType.BaitApple || type == EntityType.BaitFace)
            {
                defpool.Add(entity);
                Warn("flood of entities", ref commons);
            }
            else Ban("abuse of entities", entity);
        }
        else if (type.IsProjectile())
        {
            if (type == EntityType.Screwdriver)
                harpoon.Add(entity);
            else if (type >= EntityType.NailCommon && type <= EntityType.NailHeated)
                flashes.Add(entity);
            else
                project.Add(entity);
        }
    }
}
