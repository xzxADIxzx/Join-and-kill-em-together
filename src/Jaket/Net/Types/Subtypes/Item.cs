namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Abstract entity of any item type, except books. </summary>
public abstract class Item : OwnableEntity
{
    Agent agent;
    Float posX, posY, posZ, rotX, rotY, rotZ;
    Cache<RemotePlayer> player;
    Rigidbody rb;
    ItemIdentifier itemId;

    /// <summary> Whether the player is holding the item. </summary>
    private bool holding;
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool placed;

    public Item(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 35;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);

            w.Bool(itemId.pickedUp);
            w.Bool(itemId.Placed());
        }
        else
        {
            w.Floats(posX, posY, posZ);
            w.Floats(rotX, rotY, rotZ);

            w.Bool(holding);
            w.Bool(placed);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref posX, ref posY, ref posZ);
        r.Floats(ref rotX, ref rotY, ref rotZ);

        holding = r.Bool();
        placed = r.Bool();
    }

    #endregion
    #region logic

    public abstract Vector3 HoldRotation { get; }

    public override void Create() => Assign(Entities.Items.Make(Type, new(posX.Prev = posX.Next, posY.Prev = posY.Next, posZ.Prev = posZ.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb, true);
        agent.Get(out itemId);

        OnTransfer = () =>
        {
            player = Owner;
            if (rb && !IsOwner) rb.isKinematic = true;
        };

        itemId.onPickUp ??= new();
        itemId.onPickUp.onActivate ??= new();

        itemId.onPickUp.onActivate.AddListener(() =>
        {
            TakeOwnage();
            Networking.LocalPlayer.Holding = this;
        });

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner || player.Value == null || player.Value.Health == 0) return;

        agent.Position = holding ? player.Value.Doll.HoldPosition                        : new(posX.Get(delta),      posY.Get(delta),      posZ.Get(delta)     );
        agent.Rotation = holding ? player.Value.Doll.HookRoot.eulerAngles + HoldRotation : new(rotX.GetAngle(delta), rotY.GetAngle(delta), rotZ.GetAngle(delta));

        agent.Scale    = holding == itemId.reverseTransformSettings ? itemId.putDownScale : Vector3.one;
        itemId.pickedUp= holding;

        if (!placed && itemId.Placed())
        {
            var zones = agent.GetComponentsInParent<ItemPlaceZone>();

            agent.Parent = null;
            zones.Each(z => z.CheckItem());

            itemId.ipz = null;
        }
        if (placed && !itemId.Placed()) Physics.OverlapSphere(agent.Position, .5f, 1 << 22).Each(
            c => c.gameObject.layer == 22, // item
            c =>
            {
                var zones = c.GetComponents<ItemPlaceZone>();
                if (zones.Length > 0)
                {
                    agent.Parent = c.transform;
                    zones.Each(z => z.CheckItem());
                }
            });
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        Dest(agent.gameObject);

        if (left >= 1 && r.Bool())
            Physics.OverlapSphere(agent.Position, 5f, 1 << 22).Each(c => c.transform.Find("../ThatExplosionGif")?.gameObject.SetActive(true));

        if (left >= 2 && r.Bool())
            GameAssets.Particle("Environment/HotSand.prefab", p => Inst(p, new(8.25f, -8.25f, 74.25f), null));
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(ItemIdentifier), MethodType.Constructor)]
    [HarmonyPrefix]
    static void Start(ItemIdentifier __instance) => Events.Post(() =>
    {
        if (__instance) Entities.Items.Sync(__instance.gameObject, true);
    });

    #endregion
}
