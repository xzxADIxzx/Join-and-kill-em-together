namespace Jaket.Net.Types;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.IO;

/// <summary>
/// Gasoline droplets are not entities: their flight is pure ballistics, so one-shot spawn events suffice, just like hitscans.
/// Hits are processed by the owner of the droplets and synced via gasoline stains and enemy fuel.
/// </summary>
public static class Gasoline
{
    /// <summary> Maximum number of droplets per batch, protects against malformed packets. </summary>
    public const int BATCH_SIZE = 8;

    /// <summary> Positions and velocities of the droplets sprayed by the local player since the last tick. </summary>
    private static List<Vector3> batch = new();

    /// <summary> Sends the accumulated droplets to other players. </summary>
    public static void Flush()
    {
        if (LobbyController.Online && batch.Count > 0) Networking.Send(PacketType.Gasoline, batch.Count * 12, w => batch.Each(v => w.Vector(v)));
        batch.Clear();
    }

    /// <summary> Spawns the received droplets, they are purely visual. </summary>
    public static void Spawn(Reader r, int size)
    {
        for (int n = Mathf.Min((size - 1) / 24, BATCH_SIZE); n > 0; n--)
        {
            var obj = Entities.Projectiles.Make(EntityType.Gasoline, r.Vector());
            obj.name = "R#Gasoline";

            obj.GetComponent<Rigidbody>().velocity = r.Vector();
            obj.AddComponent<Droplet>();
        }
    }

    #region harmony

    [DynamicPatch(typeof(GasolineProjectile), nameof(GasolineProjectile.Start))]
    [Prefix]
    static void Fired(GasolineProjectile __instance)
    {
        if (__instance && __instance.name[0] != 'R')
        {
            batch.Add(__instance.transform.position);
            batch.Add(__instance.rb.velocity);
        }
    }

    [DynamicPatch(typeof(GasolineProjectile), nameof(GasolineProjectile.OnTriggerEnter))]
    [Prefix]
    static bool Touch(GasolineProjectile __instance) => __instance.name[0] != 'R';

    #endregion

    /// <summary> Component that imitates the impact behavior of a real droplet without producing any effects. </summary>
    public class Droplet : MonoBehaviour
    {
        void Start() => Destroy(gameObject, 10f);

        void OnTriggerEnter(Collider other)
        {
            if (LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment)) Destroy(gameObject);
        }
    }
}
