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

    private void Start()
    {
        SpawnEffect();
        Boss(() => Type == EntityType.Cerberus && Tools.Scene == "Level 0-5", 80f, 1, "CERBERUS, GUARDIAN OF HELL");
        Boss(() => Type == EntityType.TheCorpseOfKingMinos, 160f, 2);

        // update the original health so that the transition to the second phase happens exactly in its half
        if (Type == EntityType.TheCorpseOfKingMinos) Tools.Field<MinosBoss>("originalHealth").SetValue(GetComponent<MinosBoss>(), EnemyId.statue.health);

        if (LobbyController.IsOwner) return;
        if (Tools.Scene == "Level 2-4" && Type == EntityType.TheCorpseOfKingMinos) transform.localEulerAngles = new(0f, 90f, 0f);
        if (Tools.Scene == "Level 7-4" && Type == EntityType.SomethingWicked) gameObject.SetActive(false);
    }

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
