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
        Boss(Type == EntityType.Cerberus && Scene == "Level 0-5", 80f, 1, "CERBERUS, GUARDIAN OF HELL");
        Boss(Type == EntityType.TheCorpseOfKingMinos, 160f, 2);
        Boss(Type == EntityType.Ferryman && Scene == "Level 5-2", 90f, 2);
        Boss(Type == EntityType.Minotaur && Scene == "Level 7-1", 80f, 1);

        Boss(Type == EntityType.MinosPrime && Tools.Scene == "Level P-1", 130f, 1);
        Boss(Type == EntityType.SisyphusPrime && Tools.Scene == "Level P-2", 200f, 2);
        Boss(Type == EntityType.FleshPrison && Tools.Scene == "Level P-1", 100f, 1);
        Boss(Type == EntityType.FleshPanopticon && Tools.Scene == "Level P-2", 300f, 2);

        if (Type == EntityType.TheCorpseOfKingMinos)
        {
            // update the original health so that the transition to the second phase happens exactly in its half
            Set("originalHealth", GetComponent<MinosBoss>(), EnemyId.statue.health);
            if (!LobbyController.IsOwner) transform.localEulerAngles = new(0f, 90f, 0f);
        }
        if (Type == EntityType.Ferryman) Set("phaseChangeHealth", GetComponent<Ferryman>(), EnemyId.machine.health / 2f);
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

    #endregion
}
