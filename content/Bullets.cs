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
}