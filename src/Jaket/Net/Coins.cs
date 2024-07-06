namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Net.Types;

/// <summary> List of all living coins and methods for finding targets. </summary>
public class Coins
{
    static Transform cc => CameraController.Instance.transform;

    /// <summary> Environmental mask needed to ray cast targets. </summary>
    private static readonly int mask = LayerMaskDefaults.Get(LMD.Environment);

    /// <summary> All alive coins that can be used during a search for targets for ricochet. </summary>
    public static List<TeamCoin> Alive = new();

    /// <summary> Finds the most suitable target for a coin ricochet. </summary>
    public static Transform FindTarget(TeamCoin coin, bool enemiesOnly, out bool isPlayer, out bool isEnemy, CoinChainCache ccc = null)
    {
        Transform target = null;
        float dst = float.MaxValue;

        void Check(Transform t)
        {
            var dif = t.position - coin.transform.position;
            var newDst = dif.sqrMagnitude;
            if (newDst < dst
                && (!Physics.Raycast(coin.transform.position, dif, out var hit, Mathf.Sqrt(newDst) - .5f, mask) || hit.transform == t)
                && (!ccc || !ccc.beenHit.Contains(t.gameObject)))
            {
                target = t;
                dst = newDst;
            }
        }

        isPlayer = isEnemy = false;
        if (!enemiesOnly)
        {
            Alive.ForEach(c =>
            {
                if (!c.shot && coin.Team == c.Team) Check(c.transform);
            });
            if (target) return target;

            Alive.ForEach(c =>
            {
                if (!c.shot && coin.Team != c.Team) Check(c.transform);
            });
            if (target) return target;

            ObjectTracker.Instance.cannonballList.ForEach(b =>
            {
                if (Tools.Within(b.transform, coin.transform, 100f)) Check(b.transform);
            });
            ObjectTracker.Instance.grenadeList.ForEach(g =>
            {
                if (Tools.Within(g.transform, coin.transform, 100f) && !g.playerRiding && !g.enemy) Check(g.transform);
            });
            if (target) return target;
        }
        if (LobbyController.PvPAllowed)
        {
            Networking.EachPlayer(p =>
            {
                if (!p.Team.Ally() && p.Health > 0) Check(p.Doll.Head);
            });
            if (target)
            {
                isPlayer = true;
                return target;
            }
        }

        Networking.EachEntity(e => e is Enemy, e =>
        {
            if (e.Type.IsTargetable() && e.EnemyId && !e.Dead) Check(e.EnemyId.weakPoint?.transform ?? e.transform);
        });
        if (target)
        {
            isEnemy = true;
            return target;
        }

        if (!enemiesOnly)
        {
            foreach (var glass in GameObject.FindGameObjectsWithTag("Glass"))
            {
                if (glass.TryGetComponent<Glass>(out var g) && !g.broken) Check(g.transform);
            }
            foreach (var floor in GameObject.FindGameObjectsWithTag("GlassFloor"))
            {
                if (floor.TryGetComponent<Glass>(out var g) && !g.broken) Check(g.transform);
            }
            if (target) return target;
        }

        return target;
    }

    /// <summary> Finds the position to which the player sent a coin by punching it. </summary>
    public static bool Punchcast(out RaycastHit hit) => Physics.Raycast(cc.position, cc.forward, out hit, float.PositiveInfinity, mask);

    /// <summary> Paints the given revolver beam in the color of the given team. This must only be used with RV1 PRI. </summary>
    public static void PaintBeam(GameObject beam, Team team)
    {
        var rb = beam.GetComponent<RevolverBeam>();
        rb.noMuzzleflash = true;
        rb.hitParticle = null;
        rb.bodiesPierced = (int)team; // this field is unused by RV1 PRI so it's okay

        var lr = beam.GetComponent<LineRenderer>();
        lr.startColor = lr.endColor = team.Color();
    }
}
