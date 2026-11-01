namespace Jaket.World;

using UnityEngine;

using Jaket.Harmony;
using Jaket.Net;
using Jaket.UI.Fragments;

/// <summary> Class responsible for managing checkpoints and progression. </summary>
public static class Checkpoints
{
    /// <summary> Restarts the level. </summary>
    public static void Restart() { }

    /// <summary> Respawns the player. </summary>
    public static void Respawn(Vector3 position, float rotation, bool flash = false)
    {
        sh.ComboOver();
        sh.ResetAllFreshness();

        nm.Respawn();
        nm.GetHealth(0, true);
        nm.ActivatePlayer();

        cc.ResetCamera(rotation);
        cc.StopShake();
        fc.fistCooldown = 0f;

        Teleporter.Tp(position, flash);
    }

    #region harmony

    [DynamicPatch(typeof(CheckPoint), nameof(CheckPoint.ActivateCheckPoint))]
    [Prefix]
    static bool Activate(CheckPoint __instance)
    {
        if (sm.currentCheckPoint && sm.currentCheckPoint != __instance)
        {
            if (sm.currentCheckPoint.resetOnGetOtherCheckpoint)
                sm.currentCheckPoint.ReactivateCheckpoint();

            nm.sameCheckpointRestarts = 0;
        }
        sm.currentCheckPoint = __instance;

        if (__instance.activateEffect.RuntimeKeyIsValid() && !__instance.invisible) Inst(__instance.activateEffect.ToAsset(), nmtr.position);

        __instance.graphic.Disable();
        __instance.activated = true;

        return false;
    }

    [DynamicPatch(typeof(CheckPoint), nameof(CheckPoint.OnRespawn))]
    [Prefix]
    static bool Respawn(CheckPoint __instance)
    {
        __instance.onRestart?.Invoke();
        __instance.toActivate.Enable();

        Respawn(__instance.transform.position + __instance.transform.up * 1.25f, __instance.transform.eulerAngles.y);
        return false;
    }

    [DynamicPatch(typeof(CheckPoint), nameof(CheckPoint.Start))]
    [Prefix]
    static void Inherit(CheckPoint __instance)
    {
        __instance.rooms.Each(r =>
        {
            r.GetOrAddComponent<GoreZone>().checkpoint = __instance;
            r.GetComponentsInChildren<Bonus>(true).Each(b => b.UpdateStatsManagerReference());
        });
        __instance.rooms = [];
    }

    [DynamicPatch(typeof(ActivateArena), nameof(ActivateArena.Activate))]
    [Prefix]
    static void Inherit(ActivateArena __instance)
    {
        __instance.doors = [];
    }

    [DynamicPatch(typeof(Door), nameof(Door.Open))]
    [Prefix]
    static void Load(Door __instance)
    {
        foreach (var room in __instance.activatedRooms)
        {
            if (room.HasIdentifier(out var id)) id.Mesh.Each(m => m && !m.forceRenderingOff, m => m.enabled = true);
            else room.Add<Entity.Identifier>(id =>
            {
                id.Mesh = room.GetComponentsInChildren<Renderer>(true);
                id.Mesh.Each(m => m && !m.enabled, m => m.forceRenderingOff = true);
            });
        }
    }

    [DynamicPatch(typeof(Door), nameof(Door.Optimize))]
    [Prefix]
    static bool Unload(Door __instance)
    {
        foreach (var room in __instance.deactivatedRooms)
        {
            if (room.HasIdentifier(out var id)) id.Mesh.Each(m => m && !m.forceRenderingOff, m => m.enabled = false);
        }
        return false;
    }

    #endregion
}
