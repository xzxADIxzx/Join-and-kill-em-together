namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of all items in the game, except glasses.. </summary>
public class Item : Entity
{
    /// <summary> Item position and rotation. </summary>
    public FloatLerp x, y, z, rx, ry, rz;

    private void Awake()
    {
        gameObject.name = "Net"; // needed to prevent object looping between client and server

        // interpolations
        x = new FloatLerp();
        y = new FloatLerp();
        z = new FloatLerp();
        rx = new FloatLerp();
        ry = new FloatLerp();
        rz = new FloatLerp();

        // other
        if (LobbyController.IsOwner)
        {
            int index = Items.PlushyIndex(transform);
            if (index == -1)
            {
                Destroy(this);
                return;
            }

            Id = Entities.NextId();
            Type = (EntityType)index + 35;
        }
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(rx.Get(LastUpdate), ry.Get(LastUpdate), rz.Get(LastUpdate));
    }

    public override void Write(Writer w)
    {
        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);
    }

    public override void Damage(Reader r) { }
}
