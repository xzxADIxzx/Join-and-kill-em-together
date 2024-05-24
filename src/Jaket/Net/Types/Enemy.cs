namespace Jaket.Net.Types;

using Jaket.IO;

/// <summary> Common to all enemies in the game class. Performs a small number of functions. </summary>
public class Enemy : OwnableEntity
{
    /// <summary> Position of the enemy in the world space. </summary>
    protected FloatLerp x = new(), y = new(), z = new();

    public override void Kill(Reader r)
    {
        if (!Dead) OnDied();
        if (r != null) Kill();
    }

    /// <summary> This method is called after the death of the enemy, local or caused remotely. </summary>
    public virtual void OnDied() => base.Kill(null);

    /// <summary> This method is called only after the remote death of the enemy. </summary>
    public virtual void Kill() => EnemyId.InstaKill();
}
