namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of both encounters with V2. </summary>
public class V2 : Enemy
{
    global::V2 v2;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        v2 = GetComponent<global::V2>();
    }

    private void Start()
    {
        SpawnEffect();
        Boss(Tools.Scene == "Level 1-4" || Tools.Scene == "Level 4-4" || Tools.Scene == "Level 7-1", v2.secondEncounter ? 80f : 40f, v2.secondEncounter ? 2 : 1);

        if (Tools.Scene == "Level 4-4")
        {
            v2.knockOutHealth = EnemyId.machine.health / 2f;
            v2.escapeTarget = Tools.ObjFind("ExitTarget").transform;
        }
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
    });

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
            Tools.ObjFind("AltarStuff/Altar").SetActive(true);
            Tools.ObjFind("BigJohnatronMusic").SetActive(false);
        }
    }

    #endregion
}
