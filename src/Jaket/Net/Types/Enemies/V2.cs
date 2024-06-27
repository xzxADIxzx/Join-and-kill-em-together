namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;

/// <summary> Representation of both encounters with V2. </summary>
public class V2 : SimpleEnemy
{
    global::V2 v2;

    protected override void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        v2 = GetComponent<global::V2>();
    }

    protected override void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 1-4" || Tools.Scene == "Level 4-4" || Tools.Scene == "Level 7-1", v2.secondEncounter ? 80f : 40f, v2.secondEncounter ? 2 : 1);

        if (Tools.Scene == "Level 4-4")
        {
            v2.knockOutHealth = EnemyId.machine.health / 2f;
            v2.escapeTarget = Tools.ObjFind("ExitTarget").transform;
        }
    }

    private void OnEnable()
    {
        // the game teleports V2 and enables it when the player moves to the second part of the arena
        if (v2.firstPhase) return;

        v2.Undie();
        v2.SlideOnly(true);

        // v2 stuck in an endless cycle if this value is true
        Tools.Field<global::V2>("escaping").SetValue(v2, false);
    }

    #region entity

    public override void OnDied()
    {
        base.OnDied();
        if (v2.intro)
        {
            v2.active = false;
            v2.escapeTarget = Tools.ObjFind("EscapeTarget")?.transform;
            v2.spawnOnDeath = v2.escapeTarget?.Find("RedArmPickup").gameObject;
            EnemyId.InstaKill();

            // the second call of StartFade on the host-side can cause NullReferenceException
            if (Tools.ObjFind("Music - Versus").TryGetComponent(out Crossfade fade) && !fade.inProgress) fade.StartFade();
        }
        if (Tools.Scene == "Level 7-1")
        {
            Tools.ObjFind("AltarStuff").transform.Find("Altar").gameObject.SetActive(true);
            Tools.ObjFind("BigJohnatronMusic").SetActive(false);

            Tools.ResFind<ItemPlaceZone>(zone => zone.transform.Find("Book") != null, zone => zone.transform.position += Vector3.up * 3f);
        }
    }

    #endregion
}
