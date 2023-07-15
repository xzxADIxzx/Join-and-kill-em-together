namespace Jaket.Net.EntityTypes;

using Steamworks;

using Jaket.Content;
using Jaket.IO;

/// <summary>
/// Local enemy that exists only on the local machine.
/// When serialized will be recorded in the same way as a remote enemy.
/// </summary>
public class LocalEnemy : Entity
{
    public void Awake()
    {
        Owner = SteamClient.SteamId.Value;
        Type = (EntityType)Enemies.CopiedIndex(gameObject.name);
    }

    public override void Write(Writer w)
    {
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);
    }

    // there is no point in reading anything, because it is a local enemy
    public override void Read(Reader r) => r.Bytes(16); // skip all data
}