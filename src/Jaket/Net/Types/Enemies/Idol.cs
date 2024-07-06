namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of an idol enemy. </summary>
public class Idol : Enemy
{
    /// <summary> Synchronized target of the idol. </summary>
    private EntityProv<Enemy> target = new();

    /// <summary> Last target of the idol. </summary>
    private EnemyIdentifier lastTarget, fakeFerry;
    /// <summary> Last target id. Equals to the max value if there is no target. </summary>
    private uint lastTargetId;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
    }

    private void Start() => SpawnEffect();

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;

        transform.position = new(x.Target, y.Target, z.Target);
        if (lastTargetId != target.Id)
        {
            if (Tools.Scene == "Level 5-2") fakeFerry = Tools.ObjFind("FerrymanIntro")?.GetComponent<EnemyIdentifier>();

            lastTargetId = target.Id;
            EnemyId.idol.ChangeOverrideTarget(fakeFerry && Tools.Within(fakeFerry.transform, transform, 100f) ? fakeFerry : target.Value?.EnemyId);
        }
    });

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
                target.Id = lastTarget?.TryGetComponent<Enemy>(out var enemy) ?? false ? enemy.Id : uint.MaxValue;
            }
            w.Id(target.Id);
        }
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

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
        DeadBullet.Replace(this);
    }

    #endregion
}
