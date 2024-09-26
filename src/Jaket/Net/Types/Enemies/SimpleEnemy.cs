namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of most enemies. Synchronizes only the position of an enemy. </summary>
public class SimpleEnemy : Enemy
{
    protected virtual void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
    }

    protected virtual void Start()
    {
        SpawnEffect();
        Boss(Type == EntityType.Cerberus && Tools.Scene == "Level 0-5", 80f, 1, "CERBERUS, GUARDIAN OF HELL");
        Boss(Type == EntityType.TheCorpseOfKingMinos, 160f, 2);
        Boss(Type == EntityType.Ferryman && Tools.Scene == "Level 5-2", 90f, 2);
        Boss(Type == EntityType.Minotaur && Tools.Scene == "Level 7-1", 80f, 1);

        if (Type == EntityType.TheCorpseOfKingMinos)
        {
            // update the original health so that the transition to the second phase happens exactly in its half
            Tools.Set("originalHealth", GetComponent<MinosBoss>(), EnemyId.statue.health);
            if (!LobbyController.IsOwner) transform.localEulerAngles = new(0f, 90f, 0f);
        }
        if (Type == EntityType.Ferryman) Tools.Set("phaseChangeHealth", GetComponent<Ferryman>(), EnemyId.machine.health / 2f);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
    });

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        w.Vector(transform.position);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
    }

    public override void OnDied()
    {
        base.OnDied();
        if (Type == EntityType.Virtue) DeadBullet.Replace(this);
    }

    #endregion
}
