namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a hideous mass. </summary>
public class Shrimp : Enemy
{
    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
    }

    private void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 1-3", 175f, 1);
        Boss(Tools.Scene == "Level 6-1", 60f, 1);

        GetComponent<Mass>().crazyModeHealth = EnemyId.statue.health * .2f;
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Target, y.Target, z.Target);
    });

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        if (UpdatesCount % 16 == 0) w.Vector(transform.position);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        if (r.Length > 10) { x.Read(r); y.Read(r); z.Read(r); }
    }

    #endregion
}
