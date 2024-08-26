namespace Jaket.Net.Types;

using HarmonyLib;
using System.Linq;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of all items in the game, except glasses and books. </summary>
public class Item : OwnableEntity
{
    static FishManager fm => FishManager.Instance;

    /// <summary> Item position and rotation. </summary>
    private FloatLerp x, y, z, rx, ry, rz;
    /// <summary> Player holding the item in their hands. </summary>
    private EntityProv<RemotePlayer> player = new();

    /// <summary> Whether the player is holding the item. </summary>
    private bool holding;
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool placed;

    private void Awake()
    {
        Init(_ => Items.Type(ItemId), true);
        InitTransfer(() =>
        {
            if (Rb && !IsOwner) Rb.isKinematic = true;
            player.Id = Owner;
        });

        x = new(); y = new(); z = new();
        rx = new(); ry = new(); rz = new();
    }

    private void Start()
    {
        if (Type.IsFish() && TryGetComponent(out FishObjectReference fish))
            fm.UnlockFish(fish.fishObject = fm.recognizedFishes.Keys.ElementAt(Type - EntityType.FishOffset - 2));
    }

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead || (player.Value?.Health == 0)) return;

        transform.position = holding && player.Value != null
            ? player.Value.Doll.HoldPosition
            : new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));

        transform.eulerAngles = new(rx.GetAngel(LastUpdate), ry.GetAngel(LastUpdate), rz.GetAngel(LastUpdate));
        if (holding) transform.eulerAngles -= new Vector3(20f, 140f);

        // remove from the altar
        if (!placed && ItemId.Placed())
        {
            transform.GetComponentsInParent<ItemPlaceZone>().Do(zone => Events.Post(() => zone.CheckItem()));
            transform.SetParent(null);
            ItemId.ipz = null;
        }
        // put on the altar
        if (placed && !ItemId.Placed()) Physics.OverlapSphere(transform.position, .5f, 20971776, QueryTriggerInteraction.Collide).DoIf(
            col => col.gameObject.layer == 22,
            col =>
            {
                var zones = col.GetComponents<ItemPlaceZone>();
                if (zones.Length > 0)
                {
                    transform.SetParent(col.transform);
                    zones.Do(zone => zone.CheckItem());
                }
            });
    });

    private void OnDestroy()
    {
        if (IsOwner) NetKill();
    }

    public void PickUp()
    {
        TakeOwnage();
        Networking.LocalPlayer.HeldItem = this;

        // a special feature of my dev plushie
        if (Type == EntityType.xzxADIxzx)
            for (int i = 0; i <= 16; i++)
                Invoke(i == 16 ? "Return" : "Rotate", i * Networking.SNAPSHOTS_SPACING);
    }

    private void Rotate() => transform.Find("adi/Head").localEulerAngles = new(90f * Random.Range(0, 3), 90f * Random.Range(0, 3), 90f * Random.Range(0, 3));
    private void Return() => transform.Find("adi/Head").localEulerAngles = new(270f, Random.value < .042f ? 45f : 0f, 0f);

    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        w.Vector(transform.eulerAngles);
        w.Bool(IsOwner ? ItemId.pickedUp : holding);
        w.Bool(IsOwner ? ItemId.Placed() : placed);
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

        if (Type == EntityType.BombFish && r != null) Instantiate(GameAssets.Harmless(), transform.position, Quaternion.identity);
    }

    #endregion
}
