namespace Jaket.Net.EntityTypes;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of all items in the game, except glasses. </summary>
public class Item : Entity
{
    /// <summary> Id of the player who last held the item. </summary>
    public ulong LastOwner, Owner;
    /// <summary> Whether the player owns the item. </summary>
    public bool IsOwner => Owner == Networking.LocalPlayer.Id;

    /// <summary> Item position and rotation. </summary>
    private FloatLerp x, y, z, rx, ry, rz;
    /// <summary> Reference to the component needed to change the kinematics. </summary>
    private Rigidbody rb;

    /// <summary> Player holding an item in their hands. </summary>
    private RemotePlayer player;
    /// <summary> Whether the player is holding an item. </summary>
    private bool holding;

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
        rb = GetComponent<Rigidbody>();
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
            Owner = Networking.LocalPlayer.Id;
        }
    }

    private void Update()
    {
        // update the player holding the item
        if (LastOwner != Owner && Networking.Entities.TryGetValue(LastOwner = Owner, out var entity) && entity is RemotePlayer player) this.player = player;

        if (IsOwner) return;

        // turn off object physics so that it does not interfere with synchronization
        if (rb != null) rb.isKinematic = true;

        transform.position = holding && this.player != null
            ? this.player.usingHook ? this.player.hook.position : this.player.hookRoot.position
            : new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(rx.Get(LastUpdate), ry.Get(LastUpdate), rz.Get(LastUpdate));
    }

    public void PickUp()
    {
        Owner = Networking.LocalPlayer.Id;
        Networking.LocalPlayer.HeldItem = this;
    }

    public override void Write(Writer w)
    {
        w.Id(Owner);
        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Bool(IsOwner ? FistControl.Instance.heldObject?.gameObject == gameObject : holding);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        Owner = r.Id();
        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);
        holding = r.Bool();
    }

    public override void Damage(Reader r) {}
}
