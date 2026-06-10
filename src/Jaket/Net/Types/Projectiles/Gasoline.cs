namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;

/// <summary> Tangible entity of the gasoline projectile type. </summary>
public class Gasoline : Projectile
{
    Agent agent;

    public Gasoline(uint id, EntityType type) : base(id, type, true, true, true) { }

    #region logic

    /// <summary> Gasoline is always green, no matter the team of the player that sprayed it. </summary>
    public override void Paint(Renderer renderer) { }

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Run(MasterKill, 10f);

        // hits are processed by the owner of the projectile and then synced via gasoline stains and enemy fuel
        if (IsOwner)
            agent.gameObject.AddComponent<Sentinel>().Patron = this;
        else
            agent.Rem<SphereCollider>();
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(GasolineProjectile), nameof(GasolineProjectile.Start))]
    [Prefix]
    static void Start(GasolineProjectile __instance)
    {
        if (__instance) Entities.Projectiles.Sync(__instance.gameObject);
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
