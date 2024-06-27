namespace Jaket.Net.Types;

using Jaket.Content;

/// <summary> Representation of a gutterman. </summary>
public class Gutterman : SimpleEnemy
{
    /// <summary> Whether the corpse must be exploded at the next <i>death</i>. </summary>
    private bool toBreakCorpse;

    protected override void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
    }

    protected override void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 7-2" && transform.position.z < 400f, 30f);
    }

    #region entity

    public override void Kill()
    {
        if (toBreakCorpse)
            GetComponent<global::Gutterman>().Explode();
        else
        {
            EnemyId.InstaKill();
            toBreakCorpse = true;
        }
    }

    #endregion
}
