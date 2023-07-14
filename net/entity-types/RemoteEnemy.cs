namespace Jaket.Net;

using System.IO;
using UnityEngine;

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

    public override void Write(BinaryWriter w) // TODO replace by local entity
    {
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.position.z);
        w.Write(transform.eulerAngles.y);
    }

    public override void Read(BinaryReader r)
    {
        LastUpdate = Time.time;

        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);
    }
}
