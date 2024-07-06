namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of all items in the game, except glasses and books. </summary>
public class Item : OwnableEntity
{
    /// <summary> Item position and rotation. </summary>
    private FloatLerp x, y, z, rx, ry, rz;
    /// <summary> Player holding the item in their hands. </summary>
    private EntityProv<RemotePlayer> player = new();

    /// <summary> Whether the player is holding the item. </summary>
    private bool holding;
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool placed;
    /// <summary> Whether the item is a torch. </summary>
    private bool torch;

    private void Awake()
    {
        Init(Items.Type, true);
        InitTransfer(() =>
        {
            if (Rb && !IsOwner) Rb.isKinematic = true;
            player.Id = Owner;
        });

        x = new(); y = new(); z = new();
        rx = new(); ry = new(); rz = new();

        torch = TryGetComponent<Torch>(out _);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;

        transform.position = holding && player.Value != null
            ? player.Value.Doll.HoldPosition
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
            var colliders = Physics.OverlapSphere(transform.position, .5f, 20971776, QueryTriggerInteraction.Collide);
            foreach (var col in colliders)
            {
                if (col.gameObject.layer != 22) continue;

                if (placed && ItemId.ipz == null && col.TryGetComponent<ItemPlaceZone>(out var _))
                {
                    transform.SetParent(col.transform);
                    foreach (var zone in col.GetComponents<ItemPlaceZone>()) zone.CheckItem();
                }

                if (torch && col.TryGetComponent<Flammable>(out var flammable)) flammable.Burn(4f);
            }
        }
    });

    public void PickUp()
    {
        TakeOwnage();
        Networking.LocalPlayer.HeldItem = this;
    }

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Bool(IsOwner ? FistControl.Instance.heldObject == ItemId : holding);
        w.Bool(IsOwner ? ItemId.ipz != null : placed);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
        rx.Read(r); ry.Read(r); rz.Read(r);
        holding = r.Bool();
        placed = r.Bool();
    }

    public override void Kill(Reader r)
    {
        base.Kill(r);
        gameObject.SetActive(false);
    }

    #endregion
}
