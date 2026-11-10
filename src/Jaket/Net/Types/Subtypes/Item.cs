namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Abstract entity of any item type, except books. </summary>
public abstract class Item : OwnableEntity
{
    Agent agent;
    Float x, y, z, i, j, k;
    Cache<RemotePlayer> player;
    Rigidbody rb;
    ItemIdentifier itemId;

    /// <summary> Whether the player is holding the item. </summary>
    private bool held;
    /// <summary> Whether the item is placed on an altar. </summary>
    private bool down;

    public Item(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 30;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent.Position);
            w.Vector(agent.Rotation);
        }
        else
        {
            w.Floats(x, y, z);
            w.Floats(i, j, k);
        }

        w.Bool(held);
        w.Bool(down);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
        r.Floats(ref i, ref j, ref k);

        held = r.Bool();
        down = r.Bool();
    }

    #endregion
    #region properties

    public override bool Debuggable => agent;

    public override Vector3 DrawPos => agent.Position;

    public override Vector3 DrawRot => agent.Rotation;

    public abstract Vector3 HoldRotation { get; }

    #endregion
    #region logic

    public override void Create() => Create(Entities.Items, ref x, ref y, ref z);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out rb, true);
        agent.Get(out itemId);

        OnTransfer = () =>
        {
            player = Owner;

            if (IsOwner) return;
            if (rb) rb.isKinematic = true;

            if (fc.currentPunch.heldItem == itemId)
                fc.currentPunch.PlaceHeldObject([], null);
        };
        OnTransfer();

        itemId.onPickUp ??= new();
        itemId.onPickUp.onActivate ??= new();
        itemId.onPickUp.onActivate.AddListener(TakeOwnage);
    }

    public override void Update(float delta)
    {
        if (IsOwner)
        {
            held = itemId.pickedUp;
            down = itemId.Placed();
        }
        else itemId.pickedUp = held;

        if (IsOwner || player.Value == null) return;

        agent.Position = held ? player.Value.Doll.HoldPosition                    : new(x.GetAware(delta), y.GetAware(delta), z.GetAware(delta));
        agent.Rotation = held ? player.Value.Doll.Reel.eulerAngles + HoldRotation : new(i.GetAngle(delta), j.GetAngle(delta), k.GetAngle(delta));
        agent.Scale    = held == itemId.reverseTransformSettings ? itemId.putDownScale : Vector3.one;

        if (!down && itemId.Placed())
        {
            var zones = agent.GetComponentsInParent<ItemPlaceZone>();

            agent.Parent = null;
            zones.Each(z => z.CheckItem());

            itemId.ipz = null;
        }
        if (down && !itemId.Placed()) Physics.OverlapSphere(agent.Position, .4f, 1 << 22).Each(c => c.name == "Zone", c =>
        {
            var zones = c.GetComponentsInParent<ItemPlaceZone>();
            if (zones.Length > 0)
            {
                agent.Parent = c.transform;
                zones.Each(z => z.CheckItem());
            }
        });
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left) => Killed(r, left, agent, bits =>
    {
        if (bits[0]) Physics.OverlapSphere(agent.Position, 4f, 1 << 22).Each(c => c.DefFind("../ThatExplosionGif").Enable());

        if (bits[1]) GameAssets.Prefab("p/Environment/HotSand.prefab", p => Inst(p, new(8.25f, -8.25f, 74.25f), null));
    });

    #endregion
    #region harmony

    [DynamicPatch(typeof(ItemIdentifier), null, HarmonyLib.MethodType.Constructor)]
    [Prefix]
    static void Start(ItemIdentifier __instance) => Events.Post(() =>
    {
        if (__instance && IsReal(__instance)) Entities.Items.Sync(__instance.gameObject, true);
    });

    #endregion
}
