namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of most enemies. Synchronizes only the position of an enemy. </summary>
public class SimpleEnemy : Enemy
{
    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
    }

    private void Start() => SpawnEffect();

    private void Update()
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        w.Vector(transform.position);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
    }

    #endregion
}
