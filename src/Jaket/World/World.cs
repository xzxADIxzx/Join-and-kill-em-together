namespace Jaket.World;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.Input;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class responsible for managing world objects and actions. </summary>
public class World
{
    static NewMovement nm => NewMovement.Instance;
    static StatsManager sm => StatsManager.Instance;

    /// <summary> Size of the pool of actions' arguments. </summary>
    public const int POOL = 24;
    /// <summary> Actions that were performed previously. </summary>
    private static bool[] performed = new bool[(byte.MaxValue + 1)];
    /// <summary> Vectors that were arguments of actions. </summary>
    private static Vector2[] pos = new Vector2[(byte.MaxValue + 1) * POOL];

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLoadingStart += () =>
        {
            if (LobbyController.Offline || LobbyController.IsOwner)
                Reset();

            if (LobbyController.Online && LobbyController.IsOwner && Pending != "Main Menu")
                Networking.Send(PacketType.Level, BufferSize, WriteData);
        };

        static void Restore()
        {
            if (LobbyController.Offline) return;

            ActionList.Each(a => !a.Dynamic, a => a.Perform(default));
            ActionList.Each(a => a.Dynamic && performed[a.Identifier], a =>
            {
                for (int i = 0; i < POOL; i++)
                {
                    var pi = pos[POOL * a.Identifier + i];
                    if (pi != default) a.Perform(pi);
                }
            });
        }
        Events.OnLoad += Restore;
        Events.OnLobbyEnter += Restore;
    }

    /// <summary> Resets performed actions and their positions. </summary>
    public static void Reset() { performed.Clear(); pos.Clear(); }

    #region data

    /// <summary> Number of bytes that the world data takes in a snapshot. </summary>
    public static int BufferSize => 4 + (Pending ?? Scene).Length + Version.CURRENT.Length + performed.Count(b => b) * 2 + pos.Count(p => p != default) * 8;

    /// <summary> Writes the world data into a snapshot. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Pending ?? Scene);
        w.String(Version.CURRENT);

        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));
        w.Byte((byte)performed.Count(b => b));

        for (int i = 0, j = 0, k = 0; i < performed.Length; i++) if (performed[i])
        {
            for (j = 0; j < POOL; j++) if (pos[POOL * i + j] == default) { k = j; break; }

            w.Byte((byte)i);
            w.Byte((byte)k);

            for (j = 0; j < k; j++) { w.Float(pos[POOL * i + j].x); w.Float(pos[POOL * i + j].y); }
        }
    }

    /// <summary> Reads the world data from a snapshot. </summary>
    public static void ReadData(Reader r)
    {
        LoadScn(r.String());
        Reset();

        if (r.String() != Version.CURRENT)
        {
            LobbyController.LeaveLobby();
            Log.Info("[LOBY] Left the lobby as the owner is outdated");
        }
        else
        {
            PrefsManager.Instance.SetInt("difficulty", r.Byte());

            for (int w = r.Byte(); w > 0; w--)
            {
                byte i = r.Byte();
                byte k = r.Byte();

                performed[i] = true;
                for (int j = 0; j < k; j++) pos[POOL * i + j] = new(r.Float(), r.Float());
            }
        }
    }

    #endregion
    #region actions

    /// <summary> Returns the next available slot of an action with the given identifier. </summary>
    private static int Next(int id)
    {
        for (int i = 0; i < POOL; i++) if (pos[POOL * id + i] == default) return POOL * id + i;
        return POOL * id + POOL - 1;
    }

    /// <summary> Performs an action with the given path and position in the outer world. </summary>
    public static void Perform(string path, Vector2 p)
    {
        ActionList.Each(a =>
        {
            if (a.Dynamic && a.Path == path)
            {
                if (a.Reperformable)
                {
                    for (int i = 0; i < POOL; i++) if (pos[POOL * a.Identifier + i] == p) return false;
                    return true;
                }
                else return !performed[a.Identifier];
            }
            else return false;
        },
        a => Networking.Send(PacketType.WorldAction, 9, w =>
        {
            w.Byte((byte)a.Identifier);
            w.Float(p.x);
            w.Float(p.y);

            performed[a.Identifier] = true;
            pos[Next(a.Identifier)] = p;

            if (Version.DEBUG) Log.Debug($"[WRLD] Performed an action {a.Path}#{a.Identifier} in the outer world");
        }));
    }

    /// <summary> Performs an action with the given identifier in the inner world. </summary>
    public static void Perform(Reader r)
    {
        var id = r.Byte();
        var pi = Next(id);

        performed[id] = true;
        pos[pi] = new(r.Float(), r.Float());

        ActionList.At(id)?.Perform(pos[pi]);

        if (Version.DEBUG) Log.Debug($"[WRLD] Performed an action {ActionList.At(id).Path}#{id} received from the outer world");
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(ObjectActivator), nameof(ObjectActivator.Activate), typeof(bool))]
    [HarmonyPostfix]
    static void Activate(ObjectActivator __instance)
    {
        Perform(__instance.Path(), default);
    }

    [HarmonyPatch(typeof(Button), "Press")]
    [HarmonyPostfix]
    static void Activate(Button __instance)
    {
        Perform(__instance.Path(), default);
    }

    [HarmonyPatch(typeof(StatueActivator), "Start")]
    [HarmonyPostfix]
    static void Activate(StatueActivator __instance)
    {
        Perform("statue", new(__instance.transform.position.x, __instance.transform.position.z));
    }

    [HarmonyPatch(typeof(LimboSwitch), nameof(LimboSwitch.Pressed))]
    [HarmonyPostfix]
    static void Activate(LimboSwitch __instance)
    {
        Perform("switch", new(__instance.transform.position.x, __instance.transform.position.z));
    }

    [HarmonyPatch(typeof(Flammable), nameof(Flammable.Burn))]
    [HarmonyPostfix]
    static void Activate(Flammable __instance)
    {
        if (__instance.name == "Flammable") Perform("flammable", new(__instance.transform.position.x, __instance.transform.position.z));
    }

    [HarmonyPatch(typeof(CheckPoint), nameof(CheckPoint.ActivateCheckPoint))]
    [HarmonyPrefix]
    static bool Activate(CheckPoint __instance)
    {
        if (sm.currentCheckPoint && sm.currentCheckPoint != __instance)
        {
            if (sm.currentCheckPoint.resetOnGetOtherCheckpoint)
                sm.currentCheckPoint.ReactivateCheckpoint();

            nm.sameCheckpointRestarts = 0;
        }
        sm.currentCheckPoint = __instance;

        if (__instance.activateEffect.RuntimeKeyIsValid() && !__instance.invisible) Inst(__instance.activateEffect.ToAsset(), nm.transform.position);
        if (__instance.graphic) __instance.graphic.SetActive(false);

        __instance.activated = true;
        return false;
    }

    [HarmonyPatch(typeof(CheckPoint), nameof(CheckPoint.OnRespawn))]
    [HarmonyPrefix]
    static bool Respawn(CheckPoint __instance)
    {
        __instance.onRestart?.Invoke();
        __instance.toActivate?.SetActive(true);

        Movement.Respawn(__instance.transform.position + Vector3.up * 1.25f, __instance.transform.eulerAngles.y);
        return false;
    }

    #endregion
}
