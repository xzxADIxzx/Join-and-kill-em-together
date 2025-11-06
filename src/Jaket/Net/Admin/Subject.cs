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

    warns = new(3f, .1f),
    packets = new(48f, 32f),
    commons = new(16f, 8f);

    public Subject(uint id) => Id = id;

    /// <summary> Gives the member a warning if the given limit is exceeded. </summary>
    public void Warn(string reason, Ratekeeper limit)
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
    public void Handle() => Warn("flood of packets", packets);

    /// <summary> Handles creation of an entity. </summary>
    public void Handle(Entity entity)
    {
        var type = entity.Type;
        if (type.IsEnemy())
        {
            if (Privilege.Has)
            {
                // TODO pools
                Warn("flood of entities", commons);
            }
            else Ban("abuse of entities", entity);
        }
        else if (type.IsItem())
        {
            if (Privilege.Has || type.IsFish() || type.IsPlushie() || type == EntityType.BaitApple || type == EntityType.BaitFace)
            {
                // TODO pools
                Warn("floow of entities", commons);
            }
            else Ban("abuse of entities", entity);
        }
        else if (type.IsProjectile())
        {
            // TODO a lotta various ratekeepers
        }
    }
}
