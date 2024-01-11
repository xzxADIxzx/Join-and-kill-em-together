namespace Jaket.Patches;

using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

using Jaket.Net;
using Jaket.Net.Types;
using Jaket.World;

[HarmonyPatch(typeof(ActivateArena))]
public class ArenaPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActivateArena.Activate))]
    static void Start(ActivateArena __instance)
    {
        // do not allow the doors to close because this will cause a lot of desync
        if (LobbyController.Lobby != null) __instance.doors = new Door[0];
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnTriggerEnter")]
    static void Enter(ActivateArena __instance, Collider other, ArenaStatus ___astat)
    {
        int status = __instance.waitForStatus; // there is quite a big check caused by complex game logic that had to be repeated
        if (DisableEnemySpawns.DisableArenaTriggers || (status > 0 && (___astat == null || ___astat.currentStatus < status))) return;

        // launch the arena even when a remote player has entered it
        if (!__instance.activated && other.gameObject.name == "Net" && other.GetComponent<RemotePlayer>() != null) __instance.Activate();
    }
}

[HarmonyPatch(typeof(DoorController))]
public class DoorPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static void Enter(DoorController __instance, Collider other, Door ___dc)
    {
        // teammates do not have the Enemy tag, which is why the doors do not open
        if (other.gameObject.name == "Net" && other.TryGetComponent<RemotePlayer>(out var player) && !__instance.doorUsers.Contains(player.EnemyId))
        {
            __instance.doorUsers.Add(player.EnemyId);
            __instance.enemyIn = true;

            // unload rooms without players for optimization
            ___dc.Optimize();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerExit")]
    static void Exit(DoorController __instance, Collider other)
    {
        // you should close the doors behind you
        if (other.gameObject.name == "Net" && other.TryGetComponent<RemotePlayer>(out var player) && __instance.doorUsers.Contains(player.EnemyId))
        {
            __instance.doorUsers.Remove(player.EnemyId);
            __instance.enemyIn = __instance.doorUsers.Count > 0;
        }
    }
}

[HarmonyPatch(typeof(ObjectActivator))]
public class ActivatorPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ObjectActivator.Activate))]
    static void Activate(ObjectActivator __instance)
    {
        if (LobbyController.Lobby != null)
        {
            World.EachStatic(sa => sa.Run());
            World.EachNet(na =>
            {
                if (na.Name == __instance.name && na.Position == __instance.transform.position) World.SyncActivation(na);
            });
        }
    }
}
