namespace Jaket.Net;

using UnityEngine;

using Jaket.IO;

public class RemoteEnemy : Entity
{
    private FloatLerp x, y, z, rotation;

    public void Awake()
    {
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rotation = new FloatLerp();
    }

    public void Update()
    {
        if (LobbyController.IsOwner) return;

        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);
    }

    public override void Write(Writer w) // TODO replace by local entity
    {
        w.Vector(transform.position);
        w.Float(transform.eulerAngles.y);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);
    }
}
