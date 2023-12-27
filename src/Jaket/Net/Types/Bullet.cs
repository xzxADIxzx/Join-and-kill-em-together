namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a rocket or cannonball. </summary>
public class Bullet : OwnableEntity
{
    /// <summary> Bullet position and rotation. </summary>
    private FloatLerp x, y, z, rx, ry, rz;
    /// <summary> Reference to the component needed to change the kinematics. </summary>
    private Rigidbody rb;

    /// <summary> Grenade component. Null if the bullet is a cannonball. </summary>
    private Grenade grenade;

    /// <summary> Cannonball component. Null if the bullet is a rocket. </summary>
    private Cannonball ball;
    /// <summary> Initial speed of a cannonball. </summary>
    public float InitSpeed;

    private void Awake()
    {
        Init(_ => Bullets.EType(name));
        OnTransferred += () =>
        {
            if (rb) rb.isKinematic = !IsOwner;
            if (ball) ball.ghostCollider = !IsOwner;

            // the client must teleport the bullet, because all entities spawn at the origin
            if (!LobbyController.IsOwner) Events.Post(() =>
            {
                transform.position = new(x.target, y.target, z.target);
                transform.eulerAngles = new(rx.target, ry.target, rz.target);
                rb.velocity = transform.forward * InitSpeed;
            });
        };

        x = new(); y = new(); z = new();
        rx = new(); ry = new(); rz = new();

        rb = GetComponent<Rigidbody>();
        grenade = GetComponent<Grenade>();
        ball = GetComponent<Cannonball>();
    }

    private void Update()
    {
        if (IsOwner) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(rx.GetAngel(LastUpdate), ry.GetAngel(LastUpdate), rz.GetAngel(LastUpdate));
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);
        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Float(InitSpeed);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);
        InitSpeed = r.Float();
    }

    public override void Kill()
    {
        grenade?.Explode(harmless: true);
        ball?.Break();
    }

    #endregion
}
