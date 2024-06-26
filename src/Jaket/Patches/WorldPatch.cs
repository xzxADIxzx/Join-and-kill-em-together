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
    [HarmonyPatch(typeof(Door), nameof(Door.Optimize))]
    static bool Unload() => LobbyController.Offline;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActivateArena.Activate))]
    static void Activate(ActivateArena __instance)
    {
        // do not allow the doors to close because this will cause a lot of desync
        if (LobbyController.Online) __instance.doors = new Door[0];
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnTriggerEnter")]
    static void Enter(ActivateArena __instance, Collider other, ArenaStatus ___astat)
    {
        // there is a large check caused by complex game logic that has to be repeated
        if (DisableEnemySpawns.DisableArenaTriggers || (__instance.waitForStatus > 0 && (___astat == null || ___astat.currentStatus < __instance.waitForStatus))) return;

        // launch the arena when a remote player entered it
        if (!__instance.activated && other.name == "Net" && other.GetComponent<RemotePlayer>() != null) __instance.Activate();
    }
}

[HarmonyPatch(typeof(CheckPoint))]
public class RoomPatch
{
    /// <summary> Copy of the list of the rooms to reset. </summary>
    private static List<GameObject> rooms;
    /// <summary> Fake class needed so that the checkpoint does not recreates the rooms at the first activation. </summary>
    private class FakeList : List<GameObject> { public new int Count => 0; }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CheckPoint.ActivateCheckPoint))]
    static void Activate(CheckPoint __instance)
    {
        if (LobbyController.Online) __instance.roomsToInherit = new FakeList();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
    static void ClearRooms(CheckPoint __instance)
    {
        if (LobbyController.Online)
        {
            rooms = __instance.newRooms;
            __instance.newRooms = new();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
    static void RestoreRooms(CheckPoint __instance)
    {
        if (LobbyController.Online)
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
    static void FightStart(TramControl __instance)
    {
        // find the cart in which the player will appear after respawn
        if (LobbyController.Online && Tools.Scene == "Level 7-1") World.TunnelRoomba = __instance.transform.parent;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TramControl.SpeedUp), typeof(int))]
    static void Up(TramControl __instance) => World.SyncTram(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TramControl.SpeedDown), typeof(int))]
    static void Down(TramControl __instance) => World.SyncTram(__instance);

    [HarmonyPrefix]
    [HarmonyPatch("FixedUpdate")]
    static bool Update() => LobbyController.Offline; // disable check for player distance
}

[HarmonyPatch]
public class ActionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ObjectActivator), nameof(ObjectActivator.Activate))]
    static void Activate(ObjectActivator __instance)
    {
        if (LobbyController.Online) World.SyncAction(__instance.gameObject);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FinalDoor), nameof(FinalDoor.Open))]
    static void OpenDoor(FinalDoor __instance)
    {
        if (LobbyController.Online) World.SyncAction(__instance, 3);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    static void OpenCase(Door __instance)
    {
        var n = __instance.name;
        if (LobbyController.Online && LobbyController.IsOwner &&
           (n.Contains("Glass") || n.Contains("Cover") ||
            n.Contains("Skull") || n.Contains("Quake") ||
            Tools.Scene == "Level 3-1" || __instance.transform.parent?.parent?.name == "MazeWalls")) World.SyncAction(__instance, 4);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.SimpleOpenOverride))]
    static void OpenSpec(Door __instance)
    {
        if (LobbyController.Online && __instance.name == "BayDoor") World.SyncAction(__instance, 4);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StatueActivator), "Start")]
    static void Activate(StatueActivator __instance)
    {
        if (LobbyController.Online && LobbyController.IsOwner) World.SyncAction(__instance, 5);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BloodFiller), "FullyFilled")]
    static void FillBlood(BloodFiller __instance)
    {
        if (LobbyController.Online && LobbyController.IsOwner) World.SyncAction(__instance, 6);
    }
}
