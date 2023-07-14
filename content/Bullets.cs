namespace Jaket.Content;

using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Jaket.Net;

/// <summary> List of all bullets in the game and some useful methods. </summary>
public class Bullets
{
    /// <summary> List of all bullets in the game. </summary>
    public static List<GameObject> Prefabs = new();

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
    }

    #region index

    /// <summary> Finds enemy index by name. </summary>
    public static int Index(string name) => Prefabs.FindIndex(prefab => prefab.name == name);

    /// <summary> Finds enemy index by the name of its clone. </summary>
    public static int CopiedIndex(string name) => Index(name.Substring(0, name.IndexOf("(Clone)")));

    #endregion
    #region serialization

    /// <summary> Writes bullet to the writer. </summary>
    public static void Write(BinaryWriter w, GameObject bullet, bool hasRigidbody = false)
    {
        int index = bullet.name == "ReflectedBeamPoint(Clone)" ? Index("Revolver Beam") : CopiedIndex(bullet.name);
        if (index == -1) throw new System.Exception("Bullet index is -1 for name " + bullet.name);

        w.Write(index);

        // position
        w.Write(bullet.transform.position.x);
        w.Write(bullet.transform.position.y);
        w.Write(bullet.transform.position.z);

        // rotation
        w.Write(bullet.transform.eulerAngles.x);
        w.Write(bullet.transform.eulerAngles.y);
        w.Write(bullet.transform.eulerAngles.z);

        w.Write(hasRigidbody);
        if (hasRigidbody)
        {
            var body = bullet.GetComponent<Rigidbody>();
            w.Write(body.velocity.x);
            w.Write(body.velocity.y);
            w.Write(body.velocity.z);
        }
        else
        {
            // the data size must be constant so that Networking can read it correctly
            w.Write(0f);
            w.Write(0f);
            w.Write(0f);
        }
    }

    /// <summary> Writes bullet to the byte array. </summary>
    public static byte[] Write(GameObject bullet, bool hasRigidbody = false) => Networking.Write(w => Write(w, bullet, hasRigidbody));

    /// <summary> Reads bullet from the reader. </summary>
    public static void Read(BinaryReader r)
    {
        var obj = Object.Instantiate(Prefabs[r.ReadInt32()]);
        obj.name = "Net"; // needed to prevent object looping between client and server

        obj.transform.position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        obj.transform.eulerAngles = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        if (r.ReadBoolean()) obj.GetComponent<Rigidbody>().velocity = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }

    /// <summary> Reads bullet from the byte array. </summary>
    public static void Read(byte[] data) => Networking.Read(data, Read);

    #endregion
    #region harmony

    /// <summary> Sends the bullet to all other players if it is local, otherwise ignore. </summary>
    public static void Send(GameObject bullet, bool hasRigidbody = false)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || bullet.name == "Net") return;

        // write bullet data to send to server or clients
        byte[] data = Bullets.Write(bullet, hasRigidbody);

        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Networking.SendEvent(member.Id, data, 0));
        else
            Networking.SendEvent2Host(data, 0);
    }

    #endregion
}