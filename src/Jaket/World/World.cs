namespace Jaket.World;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.Net;

/// <summary> Class that manages objects in the level, such as hook points, skull cases, triggers and etc. </summary>
public class World
{
    /// <summary> Actions that were performed previously. </summary>
    private static bool[] performed = new bool[byte.MaxValue + 1];
    /// <summary> Vectors that were arguments of actions. </summary>
    private static Vector3[] pos = new Vector3[byte.MaxValue + 1];

    /// <summary> Subscribes to several events for proper work. </summary>
    public static void Load()
    {
        Events.OnLoadingStart += () =>
        {
            performed.Clear();
            pos.Clear();

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

    #region data

    /// <summary> Number of bytes that the world data takes in a snapshot. </summary>
    public static int BufferSize => 3 + ((Pending ?? Scene).Length + Version.CURRENT.Length) * 2;

    /// <summary> Writes data about the world such as level, difficulty and triggers fired. </summary>
    public static void WriteData(Writer w)
    {
        w.String(Pending ?? Scene);

        // the version is needed for a warning about incompatibility
        w.String(Version.CURRENT);
        w.Byte((byte)PrefsManager.Instance.GetInt("difficulty"));

        // synchronize activated actions
        // w.Bytes(Activated.ToArray());
    }

    /// <summary> Reads data about the world: loads the level, sets difficulty and fires triggers. </summary>
    public static void ReadData(Reader r)
    {
        LoadScn(r.String());

        // if the mod version doesn't match the host's one, then reading the packet is complete, as this may lead to bigger bugs
        if (r.String() != Version.CURRENT)
        {
            Bundle.Hud2NS("version.host-outdated");
            return;
        }
        PrefsManager.Instance.SetInt("difficulty", r.Byte());

        // TODO add the amount of data to the list of args
        // TODO also replace it with a list of bools to avoid collisions
        // Activated.Clear();
        // Activated.AddRange(r.Bytes(r.Length - r.Position));
    }

    #endregion
}
