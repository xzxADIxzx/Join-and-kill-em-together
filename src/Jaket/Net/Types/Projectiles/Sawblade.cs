namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any sawblade type. </summary>
public class Sawblade : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Nail nail;
    Renderer[] rs;

    public Sawblade(uint id, EntityType type) : base(id, type) { }

    #region snapshot

    public override int BufferSize => 21;

    public override void Write(Writer w)
    {
        WriteOwner(ref w);

        if (IsOwner)
            w.Vector(agent.Position);
        else
            w.Floats(x, y, z);
    }

    public override void Read(Reader r)
    {
        if (ReadOwner(ref r)) return;

        r.Floats(ref x, ref y, ref z);
    }

    #endregion
    #region logic

    public override void Create() => Assign(Entities.Projectiles.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out nail);
        agent.Get(out rs);
        agent.Rem<DestroyOnCheckpointRestart>();

        OnTransfer = () =>
        {
            player = Owner;

            rb.isKinematic = !IsOwner;
            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                    r.material.color = team.Color();
            });
        };

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.Get(delta), y.Get(delta), z.Get(delta));
        nail.punchable = true;

        if (nail.punched)
        {
            TakeOwnage();
            rb.velocity = (Punch.GetParryLookTarget() - agent.Position).normalized * 200f;
        }
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        nail.SawBreak();
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Nail), "Start")]
    [HarmonyPrefix]
    static void Start(Nail __instance)
    {
        if (__instance && __instance.sawblade && !__instance.chainsaw) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Nail), "SawBreak")]
    [HarmonyPrefix]
    static bool Break(Nail __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Sawblade s)
        {
            // it's called after death to spawn some nice particles
            if (s.Hidden) return true;

            if (s.IsOwner) s.Kill();
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Nail), "RemoveTime")]
    [HarmonyPrefix]
    static bool Death(Nail __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Sawblade s)
        {
            if (s.IsOwner) s.Kill();
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Nail), "DamageEnemy")]
    [HarmonyPrefix]
    static bool Damage(Nail __instance, EnemyIdentifier eid) => Entities.Damage.Deal<Sawblade>(__instance, (eid, tid, ally) =>
    {
        if (ally) { __instance.hitAmount += 1; return false; }

        float fodder = eid.enemyType switch
        {
            EnemyType.Filth   => 2.0f,
            EnemyType.Stray   => 2.0f,
            EnemyType.Schism  => 1.5f,
            EnemyType.Soldier => 1.5f,
            EnemyType.Stalker => 1.5f,
            _                 => 1.0f,
        };
        float damage = __instance.damage * (__instance.punched ? 2f : 1f) * (__instance.fodderDamageBoost ? fodder : 1f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, eid: eid);

    #endregion
}
