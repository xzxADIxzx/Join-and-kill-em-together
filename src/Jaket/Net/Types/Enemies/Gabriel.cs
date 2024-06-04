namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of both encounters with Gabriel. </summary>
public class Gabriel : Enemy
{
    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId));
        InitTransfer();
        Gabriel1 = GetComponent<global::Gabriel>();
        Gabriel2 = GetComponent<GabrielSecond>();
    }

    private void Start()
    {
        SpawnEffect();
        Boss(() => Tools.Scene == "Level 3-2" || Tools.Scene == "Level 6-2", 100f, 2);
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
        if (Tools.Scene == "Level 3-2")
        {
            var parent = Tools.ObjFind("GabrielOutroParent").transform;
            var outro = parent.Find("GabrielOutro").GetComponent<GabrielOutro>();

            outro.SetSource(transform);
            outro.gabe = Gabriel1;
            outro.gameObject.SetActive(true);
            gameObject.SetActive(false);

            Tools.ObjFind("Music 3").SetActive(false);
            Tools.ObjFind("Eyeball").GetComponent<AlwaysLookAtCamera>().ChangeOverrideTarget(parent.Find("gab_Intro4"));
            StatsManager.Instance.StopTimer();
        }
    }

    #endregion
}
