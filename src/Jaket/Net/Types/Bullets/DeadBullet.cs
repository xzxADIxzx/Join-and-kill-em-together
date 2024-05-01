namespace Jaket.Net.Types;

using Jaket.IO;

/// <summary> Plug designed to prevent respawn of bullets. </summary>
public class DeadBullet : Entity
{
    public static DeadBullet Instance;

    private void Awake()
    {
        Instance = this;
        Dead = true;
    }

    public override void Read(Reader r) { }
    public override void Write(Writer w) { }
}
