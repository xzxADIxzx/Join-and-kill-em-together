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
    /// <summary> Identifier used to synchronize altars. </summary>
    private ItemIdentifier itemId;

    /// <summary> Player holding an item in their hands. </summary>
    private RemotePlayer player;
    /// <summary> Whether the player is holding an item. </summary>
    private bool holding;
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool placed;

    /// <summary> Time of last transfer of the item from one client to another. </summary>
    private float lastTransferTime;

    private void Awake()
    {
        bool plushy = name.StartsWith("DevPlushie");
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
        itemId = GetComponent<ItemIdentifier>();

        if (LobbyController.IsOwner)
        {
            int index = plushy ? Items.PlushyIndex(transform) : Items.ItemIndex(itemId);
            if (index == -1)
            {
                Destroy(this);
                return;
            }

            Id = Entities.NextId();
            Type = (EntityType)(plushy ? index + 35 : index);
            Owner = Networking.LocalPlayer.Id;
        }
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
            ? this.player.usingHook ? this.player.hook.position : this.player.hookRoot.position
            : new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        transform.eulerAngles = new(rx.Get(LastUpdate), ry.Get(LastUpdate), rz.Get(LastUpdate));

        // remove from the altar
        if (!placed && itemId.ipz != null)
        {
            itemId.transform.SetParent(null);
            itemId.ipz.CheckItem();
            itemId.ipz = null;
        }

        // put on the altar
        if (placed && itemId.ipz == null)
        {
            var colliders = Physics.OverlapSphere(transform.position, 0.01f, 20971776, QueryTriggerInteraction.Collide);
            foreach (var col in colliders)
                if (col.gameObject.layer == 22 && col.TryGetComponent<ItemPlaceZone>(out var zone))
                {
                    itemId.transform.SetParent(col.transform);
                    zone.CheckItem();
                }
        }
    }

    public void PickUp()
    {
        Owner = Networking.LocalPlayer.Id;
        lastTransferTime = Time.time;
        Networking.LocalPlayer.HeldItem = this;
    }

    public override void Write(Writer w)
    {
        w.Id(Owner);
        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Bool(IsOwner ? FistControl.Instance.heldObject?.gameObject == gameObject : holding);
        w.Bool(IsOwner ? itemId.ipz != null : placed);
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

    public override void Damage(Reader r) { }
}
