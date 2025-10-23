namespace Jaket.World;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class responsible for managing world objects and actions. </summary>
public class World
{
    /// <summary> Actions that were performed previously. </summary>
    private static bool[] performed = new bool[(byte.MaxValue + 1)];
    /// <summary> Vectors that were arguments of actions. </summary>
    private static Vector2[] pos = new Vector2[(byte.MaxValue + 1) * 16];

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
                for (int i = 0; i < 16; i++)
                {
                    var pi = pos[a.Identifier + i];
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
            for (j = 0; j < 16; j++) if (pos[i + j] == default) { k = j; break; }

            w.Byte((byte)i);
            w.Byte((byte)k);

            for (j = 0; j < k; j++) { w.Float(pos[i + j].x); w.Float(pos[i + j].y); }
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
                for (int j = 0; j < k; j++) pos[i + j] = new(r.Float(), r.Float());
            }
        }
    }

    #endregion
}
