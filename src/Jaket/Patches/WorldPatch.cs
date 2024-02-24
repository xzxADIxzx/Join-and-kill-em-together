namespace Jaket.Patches;

using HarmonyLib;
using System.Collections.Generic;
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
    static void Activate(ActivateArena __instance)
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
            ___dc?.Optimize();
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

[HarmonyPatch(typeof(CheckPoint))]
public class RoomPatch
{
    /// <summary> Copy of the list of the rooms to reset. </summary>
    private static List<GameObject> rooms;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
    static void ClearRooms(CheckPoint __instance)
    {
        if (LobbyController.Lobby != null)
        {
            rooms = __instance.newRooms;
            __instance.newRooms = new();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
    static void RestoreRooms(CheckPoint __instance)
    {
        if (LobbyController.Lobby != null)
        {
            __instance.newRooms = rooms;

            __instance.onRestart?.Invoke();
            __instance.toActivate?.SetActive(true);

            var trn = __instance.transform;
            Movement.Instance.Respawn(trn.position + trn.right * .1f + Vector3.up * 1.25f, trn.eulerAngles.y);
        }
    }
}

[HarmonyPatch(typeof(TramControl))]
public class TramPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(TramControl.SpeedUp), typeof(int))]
    static void Up(TramControl __instance)
    {
        // find the cart in which the player will appear after respawn
        if (LobbyController.Lobby != null && Tools.Scene == "Level 7-1") World.Instance.TunnelRoomba = __instance.transform.parent;
    }
}

[HarmonyPatch]
public class ActionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ObjectActivator), nameof(ObjectActivator.Activate))]
    static void Activate(ObjectActivator __instance)
    {
        if (LobbyController.Lobby != null && LobbyController.IsOwner) World.EachNet(na =>
        {
            if (na.Position == __instance.transform.position && na.Name == __instance.name) World.SyncActivation(na);
        });
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalDoor), nameof(FinalDoor.Open))]
    static void OpenDoor(FinalDoor __instance)
    {
        if (LobbyController.Lobby != null) World.SyncOpening(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    static void OpenCase(Door __instance)
    {
        var name = __instance.name;
        if (LobbyController.Lobby != null && LobbyController.IsOwner &&
           (name.Contains("Case") || name.Contains("Glass") || name.Contains("Cover") || name.Contains("Skull"))) World.SyncOpening(__instance, false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WeaponPickUp), "Awake")]
    static void DropShotgun()
    {
        if (LobbyController.Lobby != null && LobbyController.IsOwner && Tools.Scene == "Level 0-3") World.SyncDrop();
    }
}
