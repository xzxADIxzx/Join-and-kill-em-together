namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.IO;

public class RemoteEnemy : Entity
{
    /// <summary> Enemy position and rotation. </summary>
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
        transform.position = new Vector3(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new Vector3(0f, rotation.GetAngel(LastUpdate), 0f);
    }

    public override void Write(Writer w) {}

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r);
        y.Read(r);
        z.Read(r);
        rotation.Read(r);
    }
}
