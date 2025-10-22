namespace Jaket.World;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class that manages objects in the level, such as hook points, skull cases, triggers and etc. </summary>
public class World
{
    /// <summary> Actions that were performed previously. </summary>
    private static bool[] performed = new bool[byte.MaxValue + 1];
    /// <summary> Vectors that were arguments of actions. </summary>
    private static Vector2[] pos = new Vector2[byte.MaxValue + 1];

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
            int counter = 0;
            ActionList.Each(a => performed[counter++] && a.Dynamic, a => a.Perform(pos[counter - 1]));
        }
        Events.OnLoad += Restore;
        Events.OnLobbyEnter += Restore;
    }

    /// <summary> Resets performed actions and their positions. </summary>
    public static void Reset() { performed.Clear(); pos.Clear(); }

    #region data

    /// <summary> Number of bytes that the world data takes in a snapshot. </summary>
    public static int BufferSize => 4 + ((Pending ?? Scene).Length + Version.CURRENT.Length) * 2 + performed.Count(b => b) * 13;

    /// <summary> Writes the world data into a snapshot. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Pending ?? Scene);
        w.String(Version.CURRENT);

        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));
        w.Byte((byte)performed.Count(b => b));

        for (int i = 0; i < performed.Length; i++) if (performed[i])
        {
            w.Byte((byte)i);
            w.Vector(pos[i]);
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

            for (byte i = r.Byte(); i > 0; i--)
            {
                byte id = r.Byte();
                performed[id] = true;
                pos[id] = r.Vector();
            }
        }
    }

    #endregion
}
