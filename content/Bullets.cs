namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;
using Jaket.UI;

/// <summary> List of all bullets in the game and some useful methods. </summary>
public class Bullets
{
    /// <summary> List of all bullets in the game. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> These objects are used as damage conventions. </summary>
    public static GameObject synchronizedBullet, networkDamage;

    /// <summary> Loads all bullets for future use. </summary>
    public static void Load()
    {
        var all = Weapons.Prefabs;
        foreach (var weapon in all)
        {
            if (weapon.TryGetComponent<Revolver>(out var revolver))
            {
                Prefabs.Add(revolver.revolverBeam);
                Prefabs.Add(revolver.revolverBeamSuper);
                Prefabs.Add(revolver.coin); // a coin is also a bullet, honestly-honestly
                continue;
            }

            if (weapon.TryGetComponent<Shotgun>(out var shotgun))
            {
                Prefabs.Add(shotgun.bullet);
                Prefabs.Add(shotgun.grenade);
                continue;
            }

            if (weapon.TryGetComponent<Nailgun>(out var nailgun))
            {
                Prefabs.Add(nailgun.nail);
                Prefabs.Add(nailgun.heatedNail);
                Prefabs.Add(nailgun.magnetNail);
                continue;
            }

            if (weapon.TryGetComponent<Railcannon>(out var railcannon))
            {
                Prefabs.Add(railcannon.beam);
                continue;
            }

            if (weapon.TryGetComponent<RocketLauncher>(out var launcher))
            {
                Prefabs.Add(launcher.rocket);
                Prefabs.Add(launcher.cannonBall?.gameObject);
                continue;
            }
        }

        // some variants are missing some projectiles
        Prefabs.RemoveAll(bullet => bullet == null);

        // create damage conventions
        synchronizedBullet = Utils.Object("Synchronized Bullet", Plugin.Instance.transform);
        networkDamage = Utils.Object("Network Damage", Plugin.Instance.transform);
    }

    #region index

    /// <summary> Finds enemy index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds enemy index by the name of its clone. </summary>
    public static int CopiedIndex(string name) => Index(name.Substring(0, name.IndexOf("(Clone)")));

    #endregion
    #region serialization

    /// <summary> Writes bullet to the writer. </summary>
    public static void Write(Writer w, GameObject bullet, bool hasRigidbody = false, bool applyOffset = true)
    {
        int index = bullet.name == "ReflectedBeamPoint(Clone)" ? Index("Revolver Beam") : CopiedIndex(bullet.name);
        if (index == -1) throw new System.Exception("Bullet index is -1 for name " + bullet.name);

        w.Int(index);

        w.Vector(bullet.transform.position + (applyOffset ? bullet.transform.forward * 2f : Vector3.zero));
        w.Vector(bullet.transform.eulerAngles);

        w.Bool(hasRigidbody);
        if (hasRigidbody)
        {
            var body = bullet.GetComponent<Rigidbody>();
            w.Vector(body.velocity);
        }
        else
        {
            // the data size must be constant so that Networking can read it correctly
            w.Vector(new Vector3(0f, 0f, 0f));
        }
    }

    /// <summary> Writes bullet to the byte array. </summary>
    public static byte[] Write(GameObject bullet, bool hasRigidbody = false, bool applyOffset = true) => Writer.Write(w => Write(w, bullet, hasRigidbody, applyOffset));

    /// <summary> Reads bullet from the reader. </summary>
    public static void Read(Reader r)
    {
        var obj = Object.Instantiate(Prefabs[r.Int()]);
        obj.name = "Net"; // needed to prevent object looping between client and server

        obj.transform.position = r.Vector();
        obj.transform.eulerAngles = r.Vector();

        if (r.Bool()) obj.GetComponent<Rigidbody>().velocity = r.Vector();
    }

    /// <summary> Reads bullet from the byte array. </summary>
    public static void Read(byte[] data) => Reader.Read(data, Read);

    #endregion
    #region harmony

    /// <summary> Sends the bullet to all other players if it is local, otherwise ignore. </summary>
    public static void Send(GameObject bullet, bool hasRigidbody = false, bool applyOffset = true)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || bullet.name.StartsWith("Net") || bullet.name.StartsWith("New")) return;

        // write bullet data to send to server or clients
        byte[] data = Write(bullet, hasRigidbody, applyOffset);

        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, data, PacketType.SpawnBullet));
        else
            Networking.Send(LobbyController.Owner, data, PacketType.SpawnBullet);
    }

    #endregion
}