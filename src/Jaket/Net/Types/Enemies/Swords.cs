namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a swordsmachine enemy. </summary>
public class Swords : Enemy
{
    SwordsMachine swords;

    /// <summary> The first phase of the boss at 0-3. </summary>
    private static Swords firstPhase;
    /// <summary> Whether the next swordsmachine will be an agony or tundra. </summary>
    private static bool agonyOrTundra;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        swords = GetComponent<SwordsMachine>();
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

        swords.phaseChangeHealth = EnemyId.machine.health / 2f;
        swords.firstPhase = !castleVein && firstPhase == null;
        swords.bothPhases = castleVein;

        if (prelude)
        {
            swords.shotgunPickUp = Instantiate(GameAssets.Shotgun());
            swords.shotgunPickUp.SetActive(false);
            Destroy(swords.shotgunPickUp.GetComponent<KeepInBounds>());
        }

        if (castleVein)
        {
            GameAssets.SwordsMaterial(agonyOrTundra ? "SwordsMachineAgony" : "SwordsMachineTundra", transform.GetChild(0).GetChild(2).GetComponent<Renderer>());
            GameAssets.SwordsMaterial(agonyOrTundra ? "SwordsMachineAgonySword" : "SwordsMachineTundraSword", transform.GetChild(0).GetChild(1).GetComponent<Renderer>());
        }

        if (Tools.Scene == "Level 0-3" && transform.position.y < 0f)
        {
            firstPhase = this; // save the object so that when you meet the enemy again, the swordsmachine has only one hand
            swords.secondPhasePosTarget = Tools.ObjFind("EnemyTracker").transform; // no matter what to put here, this is only necessary to start animation
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
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
    }

    #endregion
}
