namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.IO;

/// <summary> Plug designed to prevent respawn of bullets. </summary>
public class DeadBullet : Entity
{
    public static DeadBullet Instance;

    public static void Replace(Entity entity)
    {
        Networking.Entities[entity.Id] = Instance;
        Instance.LastUpdate = Time.time;
    }

    private void Awake()
    {
        Instance = this;
        Dead = true;
    }

    public override void Read(Reader r) { }
    public override void Write(Writer w) { }
}
