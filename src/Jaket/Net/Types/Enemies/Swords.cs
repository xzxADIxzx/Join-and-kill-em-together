namespace Jaket.Net.Types;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using UnityEngine;

/// <summary> Representation of a swordsmachine enemy. </summary>
public class Swords : Enemy
{
    /// <summary> The first phase of the boss at 0-3. </summary>
    private static SwordsMachine firstPhase;
    /// <summary> Whether the next swordsmachine will be an agony or tundra. </summary>
    private static bool agonyOrTundra;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        Swords = GetComponent<SwordsMachine>();
    }

    private void Start()
    {
        bool prelude = Tools.Scene == "Level 0-2" || Tools.Scene == "Level 0-3";
        bool castleVein = Tools.Scene == "Level 1-3";

        SpawnEffect();
        Boss(() => prelude || castleVein, prelude ? 125f : 50f, castleVein || firstPhase != null ? 1 : 2, prelude
            ? null
            : (agonyOrTundra = !agonyOrTundra)
                ? "SWORDSMACHINE \"AGONY\""
                : "SWORDSMACHINE \"TUNDRA\"");

        Swords.phaseChangeHealth = EnemyId.machine.health / 2f;
        Swords.firstPhase = !castleVein && firstPhase == null;
        Swords.bothPhases = castleVein;

        if (prelude)
        {
            Swords.shotgunPickUp = Instantiate(GameAssets.Shotgun());
            Swords.shotgunPickUp.SetActive(false);

            // save the object so that when you meet the enemy again, the swordsmachine has only one hand
            firstPhase = Swords;
        }

        if (castleVein)
        {
            GameAssets.SwordsMaterial(agonyOrTundra ? "SwordsMachineAgony" : "SwordsMachineTundra", transform.GetChild(0).GetChild(2).GetComponent<Renderer>());
            GameAssets.SwordsMaterial(agonyOrTundra ? "SwordsMachineAgonySword" : "SwordsMachineTundraSword", transform.GetChild(0).GetChild(1).GetComponent<Renderer>());
        }
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
