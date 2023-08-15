namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.World;

[HarmonyPatch(typeof(NewMovement), "Start")]
public class SpawnPatch
{
    // add some randomness to the spawn position so players don't stack on top of each other at the start of the level
    static void Prefix(NewMovement __instance) => __instance.transform.position += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
}

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
public class RespawnPatch
{
    static void Prefix()
    {
        // checkpoint destroys some objects on the level
        World.Instance.Invoke("Recache", .1f);

        // in the sandbox after death, enemies are not destroyed
        if (LobbyController.Lobby == null || !LobbyController.IsOwner || SceneHelper.CurrentScene == "uk_construct") return;

        // notify each client that the host has died so that they destroy all enemies
        LobbyController.EachMemberExceptOwner(member => Networking.SendEmpty(member.Id, PacketType.HostDied));
    }
}
