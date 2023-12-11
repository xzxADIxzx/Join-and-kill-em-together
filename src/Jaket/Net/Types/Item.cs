namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of all items in the game, except glasses. </summary>
public class Item : Entity
{
    /// <summary> Id of the player who last held the item. </summary>
    public ulong LastOwner, Owner = LobbyController.LastOwner;
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
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool placed;
    /// <summary> Whether the item is a torch. </summary>
    private bool torch;

    /// <summary> Time of last transfer of the item from one client to another. </summary>
    private float lastTransferTime;

    private void Awake()
    {
        Init(Items.Type);

        x = new(); y = new(); z = new();
        rx = new(); ry = new(); rz = new();

        rb = GetComponent<Rigidbody>();
        torch = GetComponent<Torch>() != null;
    }

    private void Update()
    {
        // update the player holding the item
        if (LastOwner != Owner && Networking.Entities.TryGetValue(LastOwner = Owner, out var entity) && entity is RemotePlayer player) this.player = player;

        // the game itself will update everything for the owner of the item
        if (IsOwner) return;

        // turn off object physics so that it does not interfere with synchronization
        if (rb != null) rb.isKinematic = true;

        transform.position = holding && this.player != null
            ? this.player.HoldPosition()
            : new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(rx.GetAngel(LastUpdate), ry.GetAngel(LastUpdate), rz.GetAngel(LastUpdate));

        // remove from the altar
        if (!placed && ItemId.ipz != null)
        {
            transform.SetParent(null);
            ItemId.ipz.CheckItem();
            ItemId.ipz = null;
        }
        // put on the altar or light the torches
        if ((placed && ItemId.ipz == null) || torch)
        {
            var colliders = Physics.OverlapSphere(transform.position, 0.5f, 20971776, QueryTriggerInteraction.Collide);
            foreach (var col in colliders)
            {
                if (col.gameObject.layer != 22) continue;

                if (placed && ItemId.ipz == null && col.TryGetComponent<ItemPlaceZone>(out var zone))
                {
                    transform.SetParent(col.transform);
                    zone.CheckItem();
                }

                if (torch && col.TryGetComponent<Flammable>(out var flammable)) flammable.Burn(4f);
            }
        }
    }

    public void PickUp()
    {
        Owner = Networking.LocalPlayer.Id;
        lastTransferTime = Time.time;
        Networking.LocalPlayer.HeldItem = this;
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Id(Owner);
        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Bool(IsOwner ? FistControl.Instance.heldObject?.gameObject == gameObject : holding);
        w.Bool(IsOwner ? ItemId.ipz != null : placed);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        ulong id = r.Id(); // this is necessary so that clients don't fight for ownership of the item
        if (Owner != id && Time.time > lastTransferTime + 1f)
        {
            Owner = id;
            lastTransferTime = Time.time;
        }

        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);
        holding = r.Bool();
        placed = r.Bool();
    }

    #endregion
}
