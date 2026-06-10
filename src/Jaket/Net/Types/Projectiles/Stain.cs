namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary> Tangible entity of the gasoline stain type. </summary>
public class Stain : OwnableEntity
{
    Agent agent;
    GasolineStain stain;

    /// <summary> Placement of the stain, it never moves relative to the surface it is attached to. </summary>
    private Vector3 pos, rot;
    /// <summary> Number of snapshots written since the last wake-up. </summary>
    private int sent;

    public Stain(uint id, EntityType type) : base(id, type) { }

    /// <summary> Stains never change, so they fall asleep once reliably delivered to everyone. </summary>
    public override bool Dormant => sent >= 16;

    /// <summary> Wakes the stain up, forcing it to broadcast itself again. </summary>
    public void Wake() => sent = 0;

    #region snapshot

    public override int BufferSize => 33;

    public override void Write(Writer w)
    {
        sent++;
        WriteOwner(ref w);

        if (IsOwner)
        {
            w.Vector(agent ? agent.Position : pos);
            w.Vector(agent ? agent.Rotation : rot);
        }
        else
        {
            w.Vector(pos);
            w.Vector(rot);
        }
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        pos = r.Vector();
        rot = r.Vector();
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, pos).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out stain);

        OnTransfer = () =>
        {
            Wake();
            if (IsOwner && agent) agent.gameObject.GetOrAddComponent<Sentinel>().Patron = this;
        };

        if (IsOwner)
        {
            pos = agent.Position;
            rot = agent.Rotation;
        }
        else
        {
            agent.Rotation = rot;
            Attach();
        }
        OnTransfer();
    }

    /// <summary> Finds the surface beneath the stain and attaches the stain to it. </summary>
    private void Attach()
    {
        var forward = agent.transform.forward;

        if (Physics.Raycast(agent.Position - forward, forward, out var hit, 3f, EnvMask))
        {
            // clipping depends on local graphics settings, so each machine resolves it on its own
            bool clip = PostProcessV2_Handler.Instance.usedComputeShadersAtStart
                && !(hit.collider.TryGetComponent(out MeshRenderer r) && (r.sharedMaterial?.IsKeywordEnabled("VERTEX_DISPLACEMENT") ?? false));

            stain.AttachTo(hit.collider, clip);
        }
        else Dest(agent.gameObject);
    }

    public override void Update(float delta) { }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        if (agent) Dest(agent.gameObject);
    }

    #endregion
    #region ignition

    /// <summary> Whether an ignition is being applied from the network. </summary>
    public static bool Igniting;

    /// <summary> Applies a remote ignition to the local stains. </summary>
    public static void Ignite(Reader r)
    {
        Igniting = true;
        StainVoxelManager.Instance.TryIgniteAt(r.Vector(), r.Byte());
        Igniting = false;
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(GasolineStain), nameof(GasolineStain.AttachTo))]
    [Postfix]
    static void Attach(GasolineStain __instance)
    {
        if (__instance) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [DynamicPatch(typeof(StainVoxelManager), nameof(StainVoxelManager.TryIgniteAt), typeof(Vector3), typeof(int))]
    [Postfix]
    static void Ignite(Vector3 worldPosition, int checkSize, bool __result)
    {
        if (__result && !Igniting) Networking.Send(PacketType.Ignition, 13, w =>
        {
            w.Vector(worldPosition);
            w.Byte((byte)checkSize);
        });
    }

    #endregion

    /// <summary> Component that reports the destruction of its object to the entity. </summary>
    public class Sentinel : MonoBehaviour
    {
        /// <summary> Entity that owns the sentinel and has to be killed on destruction. </summary>
        public Entity Patron;

        void OnDestroy()
        {
            if (gameObject.scene.isLoaded && !Networking.Loading && LobbyController.Online && Patron.IsOwner && !Patron.Hidden) Patron.Kill();
        }
    }
}
