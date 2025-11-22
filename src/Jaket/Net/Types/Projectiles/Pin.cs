namespace Jaket.Net.Types;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.IO;

/// <summary> Tangible entity of any nail type. </summary>
public class Pin : OwnableEntity
{
    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Renderer[] rs;
    CapsuleCollider cont;

    public Pin(uint id, EntityType type) : base(id, type) { }

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
        agent.Get(out rs);
        agent.Get(out cont);

        OnTransfer = () =>
        {
            player = Owner;

            rb.isKinematic = !IsOwner;
            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                    r.material.color = team.Color() * 10f;
            });

            cont.enabled = IsOwner;
        };

        Locked = false;

        OnTransfer();
    }

    public override void Update(float delta)
    {
        if (IsOwner) return;

        agent.Position = new(x.Get(delta), y.Get(delta), z.Get(delta));
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        Hidden = true;
        if (agent) Dest(agent.gameObject);
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Nail), "Start")]
    [HarmonyPrefix]
    static void Start(Nail __instance)
    {
        if (__instance && !__instance.sawblade && !__instance.chainsaw) Entities.Projectiles.Sync(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Nail), "OnDestroy")]
    [HarmonyPrefix]
    static void Death(Nail __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is Pin p && !p.Hidden) p.Kill();
    }

    [HarmonyPatch(typeof(Punch), "BlastCheck")]
    [HarmonyPrefix]
    static void Blast(bool ___holdingInput)
    {
        if (___holdingInput) Networking.Entities.Alive<Pin>(p => p.rb, p =>
        {
            var dir = p.agent.Position - NewMovement.Instance.transform.position;
            if (dir.sqrMagnitude < 100f)
            {
                p.TakeOwnage();
                p.rb.velocity = dir.normalized * 100f;
            }
        });
    }

    [HarmonyPatch(typeof(Nail), "DamageEnemy")]
    [HarmonyPrefix]
    static bool Damage(Nail __instance, EnemyIdentifier eid) => Entities.Damage.Deal<Pin>(__instance, (eid, tid, ally, _) =>
    {
        if (ally) return false;

        float fodder = eid.enemyType switch
        {
            EnemyType.Filth   => 2.0f,
            EnemyType.Stray   => 2.0f,
            EnemyType.Schism  => 1.5f,
            EnemyType.Soldier => 1.5f,
            EnemyType.Stalker => 1.5f,
            _                 => 1.0f,
        };
        float damage = __instance.damage * (__instance.fodderDamageBoost ? fodder : 1f);

        Entities.Damage.Deal(tid, damage);
        return true;
    }, eid: eid);

    #endregion
}
