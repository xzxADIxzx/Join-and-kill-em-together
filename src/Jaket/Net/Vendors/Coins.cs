namespace Jaket.Net.Vendors;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Net.Types;

using static Entities;

/// <summary> Vendor responsible for coins. </summary>
public class Coins : Vendor
{
    public void Load()
    {
        byte type = (byte)EntityType.Coin;

        GameAssets.Prefab("Attacks and Projectiles/Coin.prefab", p => Vendor.Prefabs[type] = p);

        Vendor.Suppliers[type] = (id, type) => new TeamCoin(id, type);
    }

    public EntityType Type(GameObject obj) => obj.name == "Coin(Clone)" ? EntityType.Coin : EntityType.None;

    public GameObject Make(EntityType type, Vector3 position = default, Transform parent = null)
    {
        if (type != EntityType.Coin) return null;

        var obj = Inst(Vendor.Prefabs[(byte)type], position);

        return obj;
    }

    public void Sync(GameObject obj, params bool[] args)
    {
        var type = Type(obj);
        if (type == EntityType.None || obj.GetComponent<Entity.Agent>()) return;

        var entity = Supply(type);

        entity.Owner = AccId;
        entity.Assign(obj.AddComponent<Entity.Agent>());
        entity.Push();
    }

    #region targeting

    static Transform cc => CameraController.Instance.transform;

    /// <summary> Finds the most suitable target of a ricochet. </summary>
    public Transform FindTarget(TeamCoin coin, bool enemiesOnly, out bool isPlayer, out bool isEnemy, CoinChainCache ccc = null)
    {
        Transform target = null;
        var max = float.PositiveInfinity;

        void Check(Transform t)
        {
            var dir = t.position - coin.Transform.position;
            var dst = dir.sqrMagnitude;
            if (dst < max
            && (!Physics.Raycast(coin.Transform.position, dir, out var hit, Mathf.Sqrt(dst) - .5f, EnvMask) || hit.transform == t)
            && (!ccc || !ccc.beenHit.Contains(t.gameObject)))
            {
                target = t;
                max = dst;
            }
        }
        bool Within(Component c) => (c.transform.position - coin.Transform.position).sqrMagnitude < 100f * 100f;

        isPlayer = isEnemy = false;
        if (!enemiesOnly)
        {
            Networking.Entities.Alive<TeamCoin>(c => c.Owner == coin.Owner, c => Check(c.Transform));
            if (target) return target;

            ObjectTracker.Instance.cannonballList.Each(Within,                                        b => Check(b.transform));
            ObjectTracker.Instance.grenadeList   .Each(g => Within(g) && !g.playerRiding && !g.enemy, g => Check(g.transform));
            if (target) return target;
        }
        if (LobbyConfig.PvPAllowed)
        {
            Networking.Entities.Player(p => p.Health > 0 && !p.Team.Ally(), p => Check(p.Doll.Head));
            if (target)
            {
                isPlayer = true;
                return target;
            }
        }
        {
            Networking.Entities.Alive<Enemy>(e => e.Type != EntityType.Idol, e => Check(e.WeakPoint));
            if (target)
            {
                isEnemy = true;
                return target;
            }
        }
        if (!enemiesOnly)
        {
            GameObject.FindGameObjectsWithTag("Glass")     .Each(o => o.TryGetComponent(out Glass g) && !g.broken, o => Check(o.transform));
            GameObject.FindGameObjectsWithTag("GlassFloor").Each(o => o.TryGetComponent(out Glass g) && !g.broken, o => Check(o.transform));
        }
        return target;
    }

    /// <summary> Finds the target position of a punchflection. </summary>
    public bool Punchcast(out RaycastHit hit) => Physics.Raycast(cc.position, cc.forward, out hit, float.PositiveInfinity, EnvMask);

    #endregion
}
