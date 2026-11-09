namespace Jaket.World;

using UnityEngine;

using Jaket.Content;
using Jaket.Harmony;
using Jaket.Net;
using Jaket.UI.Fragments;

/// <summary> Class responsible for managing checkpoints and progression. </summary>
public static class Checkpoints
{
    #region mission

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

    #endregion
    #region loaders

    /// <summary> Returns the mesh of the room. </summary>
    public static Renderer[] Mesh(Transform room)
    {
        room.Each(c => { if (c.name.Contains("Nonstuff")) room = c; });

        var mesh = room.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < mesh.Length; i++) if (mesh[i].GetComponentInParent<ItemIdentifier>(true)) mesh[i] = null;

        return mesh;
    }

    /// <summary> Marks the room and its mesh. </summary>
    public static void Mark(GameObject room)
    {
        if (room && room.HasIdentifier(out var id))
        {
            id.Type = EntityType.Room;

            id.Mesh = Mesh(room.transform);

            id.Mesh.Each(m => m && !m.enabled, m => m.forceRenderingOff = true);
        }
    }

    /// <summary> Loads the room and its mesh. </summary>
    public static void Load(GameObject room, bool visible, string reason)
    {
        if (room && room.HasIdentifier(out var id) && id.Type.IsRoom())
        {
            id.Mesh.Each(m => m && !m.forceRenderingOff, m => m.enabled = visible);

            if (visible) room.Enable();

            if (Version.DEBUG) Log.Debug($"[WRLD] {(visible ? "Loaded" : "Unloaded")} room {room.name} due to {reason}");
        }
    }

    #endregion
    #region harmony

    [DynamicPatch(typeof(CheckPoint), nameof(CheckPoint.ActivateCheckPoint))]
    [Prefix]
    static bool Reached(CheckPoint __instance)
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

    [DynamicPatch(typeof(Door), nameof(Door.Awake))]
    [Prefix]
    static void Inherit(Door __instance)
    {
        __instance.activatedRooms.Each(r => !r.HasIdentifier(), r => r.Add<Entity.Identifier>(i => Mark(i.gameObject)));
    }

    [DynamicPatch(typeof(Door), nameof(Door.Open))]
    [Prefix]
    static bool Load(Door __instance)
    {
        __instance.activatedRooms.Each(r => Load(r, true, "a door"));
        return true;
    }

    [DynamicPatch(typeof(Door), nameof(Door.Optimize))]
    [Prefix]
    static bool Unload(Door __instance)
    {
        __instance.deactivatedRooms.Each(r => Load(r, false, "a door"));
        return false;
    }

    [DynamicPatch(typeof(UltrakillEvent), nameof(UltrakillEvent.Invoke))]
    [Prefix]
    static bool Load(UltrakillEvent __instance)
    {
        __instance.toDisActivateObjects.Each(r =>
        {
            if (r && r.name.Contains('-')) Load(r, false, "an event");
            else r.Disable();
        });
        __instance.toActivateObjects.Each(r =>
        {
            if (r && r.name.Contains('-')) Load(r, true, "an event");
            else r.Enable();
        });
        __instance.onActivate?.Invoke();
        return false;
    }

    [DynamicPatch(typeof(UltrakillEvent), nameof(UltrakillEvent.Revert))]
    [Prefix]
    static bool Unload(UltrakillEvent __instance)
    {
        __instance.toDisActivateObjects.Each(r =>
        {
            if (r && r.name.Contains('-')) Load(r, true, "an event");
            else r.Enable();
        });
        __instance.toActivateObjects.Each(r =>
        {
            if (r && r.name.Contains('-')) Load(r, false, "an event");
            else r.Disable();
        });
        __instance.onDisActivate?.Invoke();
        return false;
    }

    #endregion
}
