namespace Jaket.Net.Types;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a turret enemy. </summary>
public class Turret : Enemy
{
    /// <summary> Id of running animation. </summary>
    /// <see cref="UnityEngine.Animator.StringToHash(string)"/>
    public const int RUNNING_ID = -526601997;

    /// <summary> Whether the turret is aiming or running. </summary>
    private bool aiming, lastAiming, running;

    private void Awake()
    {
        Init(_ => Enemies.Type(EnemyId), true);
        InitTransfer(() => Cooldown(IsOwner ? 0f : 4200f));
        Turret = GetComponent<global::Turret>();
    }

    private void Start() => SpawnEffect();

    private void Update()
    {
        if (IsOwner || Dead) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        Animator.SetBool(RUNNING_ID, running);

        if (lastAiming != aiming)
        {
            if (lastAiming = aiming)
                Turret.Invoke("StartWindup", 0f);
            else
                Cooldown(4200f);
        }
    }

    private void Cooldown(float time) => Tools.Field<global::Turret>("cooldown").SetValue(Turret, time);

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        w.Bool(Turret.aiming);
        if (!Turret.aiming)
        {
            w.Bool(Animator.GetBool(RUNNING_ID));
            w.Vector(transform.position);
        }
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        aiming = r.Bool();
        if (!aiming)
        {
            running = r.Bool();
            x.Read(r); y.Read(r); z.Read(r);
        }
    }

    #endregion
}
