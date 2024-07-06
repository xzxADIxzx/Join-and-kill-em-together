namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a rocket or cannonball. </summary>
public class Bullet : OwnableEntity
{
    Grenade grenade;
    Cannonball ball;

    /// <summary> Bullet position and rotation. </summary>
    private FloatLerp x, y, z, rx, ry, rz;
    /// <summary> Player riding the rocket. </summary>
    private EntityProv<RemotePlayer> player = new();

    /// <summary> Whether the rocket is frozen by the owner. </summary>
    private bool frozen;
    /// <summary> Whether the player is currently riding the rocket. </summary>
    private bool riding;

    private void Awake()
    {
        Init(_ => Bullets.EType(name), true);
        InitTransfer(() =>
        {
            if (Rb) Rb.isKinematic = !IsOwner;
            if (ball) ball.ghostCollider = !IsOwner;
            if (grenade)
            {
                if (IsOwner) grenade.rocketSpeed = 100f;
                Exploded(!IsOwner);
            }
            player.Id = Owner;
        });
        TryGetComponent(out grenade);
        TryGetComponent(out ball);
        Destroy(GetComponent<FloatingPointErrorPreventer>());

        x = new(); y = new(); z = new();
        rx = new(); ry = new(); rz = new();
    }

    private void Start() => ClearTrail(GetComponentInChildren<TrailRenderer>(), x, y, z);

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;

        if (riding)
        {
            transform.localPosition = Vector3.back;
            transform.localEulerAngles = Vector3.zero;

            if (transform.parent != player.Value.transform) transform.SetParent(player.Value.transform, true);
        }
        else
        {
            transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
            transform.eulerAngles = new(rx.GetAngel(LastUpdate), ry.GetAngel(LastUpdate), rz.GetAngel(LastUpdate));

            if (transform.parent != null) transform.SetParent(null, true);
        }
    });

    private void Exploded(bool value) => Tools.Set("exploded", grenade, value);

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);

        if (grenade)
        {
            w.Bool(IsOwner ? grenade.playerRiding : riding);
            w.Bool(IsOwner ? grenade.frozen : frozen);
        }
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);

        if (grenade)
        {
            riding = r.Bool();
            grenade.rocketSpeed = (frozen = r.Bool()) ? 98f : 99f;
        }
    }

    public override void Kill(Reader r)
    {
        DeadBullet.Replace(this);
        if (grenade)
        {
            Exploded(false);

            if (r != null && r.Position < r.Length)
                grenade.Explode(true, sizeMultiplier: r.Bool() ? 2f : 1f);
            else
                grenade.Explode(harmless: true);
        }
        else ball?.Break();
    }

    #endregion
}
