namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of both encounters with V2. </summary>
public class V2 : Enemy
{
    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        V2 = GetComponent<global::V2>();
    }

    private void Start()
    {
        SpawnEffect();
        Boss(() => V2.intro, V2.secondEncounter ? 80f : 40f, V2.secondEncounter && V2.firstPhase ? 2 : 1);
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

    public override void OnDied()
    {
        base.OnDied();
        if (V2.intro)
        {
            V2.active = false;
            V2.escapeTarget = Tools.ObjFind("EscapeTarget")?.transform;
            V2.spawnOnDeath = V2.escapeTarget?.Find("RedArmPickup").gameObject;
            EnemyId.InstaKill();

            // the second call of StartFade on the host-side can cause NullReferenceException
            if (Tools.ObjFind("Music - Versus").TryGetComponent(out Crossfade fade) && !fade.inProgress) fade.StartFade();
        }
    }

    #endregion
}
