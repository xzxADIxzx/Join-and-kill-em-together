namespace Jaket.Net;

using System.Collections.Generic;
using UnityEngine;

using Jaket.Content;
using Jaket.Net.Types;

/// <summary> List of all living coins and methods for finding targets. </summary>
public class Coins
{
    /// <summary> Environmental mask needed to ray cast targets. </summary>
    private static readonly int mask = LayerMaskDefaults.Get(LMD.Environment);

    /// <summary> All alive coins that can be used during a search for targets for ricochet. </summary>
    public static List<TeamCoin> Alive = new();

    /// <summary> Finds the most suitable target for a coin ricochet. </summary>
    public static Transform FindTarget(TeamCoin coin, bool enemiesOnly, out bool isPlayer, out bool isEnemy)
    {
        Transform target = null;
        float dst = float.MaxValue;

        void Check(Transform t)
        {
            var dif = t.position - coin.transform.position;
            var newDst = dif.sqrMagnitude;
            if (newDst < dst && !Physics.Raycast(coin.transform.position, dif, Mathf.Sqrt(newDst) - .5f, mask))
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
                if (coin.CanHit(c) && coin.Team == c.Team) Check(c.transform);
            });
            if (target) return target;

            Alive.ForEach(c =>
            {
                if (coin.CanHit(c) && coin.Team != c.Team) Check(c.transform);
            });
            if (target) return target;

            ObjectTracker.Instance.cannonballList.ForEach(b =>
            {
                if ((b.transform.position - coin.transform.position).sqrMagnitude < 10000f) Check(b.transform);
            });
            ObjectTracker.Instance.grenadeList.ForEach(g =>
            {
                if ((g.transform.position - coin.transform.position).sqrMagnitude < 10000f && !g.playerRiding && !g.enemy) Check(g.transform);
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
            if (e.EnemyId) Check(e.EnemyId.weakPoint?.transform ?? e.transform);
        });
        if (target)
        {
            isEnemy = true;
            return target;
        }

        // breakables such as glass panels

        return target;
    }
}
