namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representations of an idol enemy. </summary>
public class Idol : Enemy
{
    /// <summary> Synchronized target of the idol. </summary>
    private EntityProv<Enemy> target = new();

    /// <summary> Last target of the idol. </summary>
    private EnemyIdentifier lastTarget;
    /// <summary> Last target id. Equals to the max value if there is no target. </summary>
    private uint lastTargetId = uint.MaxValue;

    private void Awake() => Init(_ => Enemies.Type(EnemyId));

    private void Update()
    {
        if (IsOwner || Dead) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        if (lastTargetId != target.Id)
        {
            lastTargetId = target.Id;
            EnemyId.idol.ChangeOverrideTarget(target.Value?.EnemyId);
        }
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        if (UpdatesCount % 16 == 0)
            w.Vector(transform.position);
        else
        {
            if (lastTarget != EnemyId.idol.target)
            {
                lastTarget = EnemyId.idol.target;
                target.Id = EnemyId.idol.target?.TryGetComponent<Enemy>(out var enemy) ?? false ? enemy.Id : uint.MaxValue;
            }
            w.Id(target.Id);
        }
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (r.Length > 14)
        {
            x.Read(r); y.Read(r); z.Read(r);
        }
        else
            target.Id = r.Id();
    }

    public override void OnDied()
    {
        base.OnDied(); // after the death of the idol, its game object is destroyed
        Networking.Entities[Id] = DeadBullet.Instance;
    }

    #endregion
}
