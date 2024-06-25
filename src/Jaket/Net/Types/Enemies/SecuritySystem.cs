namespace Jaket.Net.Types;

/// <summary> Representation of a part of the security system. </summary>
public class SecuritySystem : Enemy
{
    private void Awake() => Init(_ => Type);

    private void Start() => Boss(true, 15f, 0);

    #region entity

    public override void OnDied()
    {
        base.OnDied();
        DeadBullet.Replace(this);
    }

    #endregion
}
