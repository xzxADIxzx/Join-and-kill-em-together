namespace Jaket.Net.Types;

using HarmonyLib;
using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

using static Jaket.UI.Lib.Pal;

/// <summary> Tangible entity of the coin type. </summary>
public class TeamCoin : OwnableEntity
{
    static Transform cc => CameraController.Instance.transform;
    static StyleHUD sh => StyleHUD.Instance;

    Agent agent;
    Float x, y, z;
    Cache<RemotePlayer> player;
    Team team => player.Value?.Team ?? Networking.LocalPlayer.Team;
    Rigidbody rb;
    Coin coin;
    Renderer[] rs;
    Collider[] cs;
    AudioSource source;

    /// <summary> Whether the coin will reflect a beam twice on hit. </summary>
    private bool doubled;
    /// <summary> Beam to be reflected after a short period of time. </summary>
    private GameObject beam;

    /// <summary> Target to be hit on reflect invoke. </summary>
    private Transform target;
    /// <summary> Targets that have already been hit. </summary>
    private CoinChainCache ccc;

    /// <summary> Whether the target is a player. </summary>
    private bool isPlayer;
    /// <summary> Whether the target is an enemy. </summary>
    private bool isEnemy;

    /// <summary> Effect showing the current state of the coin. </summary>
    private GameObject effect;
    /// <summary> Power that increases after punch or ricochet. </summary>
    private int power = 2;

    public TeamCoin(uint id, EntityType type) : base(id, type) { }

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

    public override void Create() => Assign(Entities.Coins.Make(Type, new(x.Prev = x.Next, y.Prev = y.Next, z.Prev = z.Next)).AddComponent<Agent>());

    public override void Assign(Agent agent)
    {
        (this.agent = agent).Patron = this;

        agent.Get(out rb);
        agent.Get(out coin);
        agent.Get(out rs);
        agent.Get(out cs);
        agent.Get(out source);

        OnTransfer = () =>
        {
            player = Owner;

            rs.Each(r =>
            {
                if (r is TrailRenderer t)
                    t.startColor = team.Color() with { a = .4f };
                else
                {
                    r.material.mainTexture = ModAssets.CoinTexture;
                    r.material.color = team.Color();
                }
            });

            Reset();
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

        if (left >= 5)
        {
            Locked = false;
            ReadOwner(ref r);
            Reset();
            if (r.Bool()) Quadruple();
        }
        else
        {
            Inst(coin.coinBreak, agent.Position);
            Dest(agent.gameObject);
        }
    }

    public void Beam(params Vector3[] positions) => Inst(coin.refBeam, agent.Position).GetComponents<LineRenderer>().Each(l =>
    {
        l.startColor = l.endColor = team.Color();
        l.SetPositions(positions);
    });

    public void Reflect(GameObject beam)
    {
        if (Hidden)
        {
            if (!team.Ally())
            {
                this.beam = beam;
                target = player.Value.Doll.Head.transform;
                isPlayer = true;
                isEnemy = false;

                agent.StopAllCoroutines();
                Reflect();
            }
            return;
        }
        if (!coin.enabled) return;

        this.beam = beam;

        target = Entities.Coins.FindTarget(this, false, out isPlayer, out isEnemy, ccc ??= Create<CoinChainCache>("Chain"));

        Kill(5, w => { w.Id(AccId); w.Bool(isPlayer); });

        agent.StartCoroutine(CallDelayed(Reflect, isPlayer ? 1.2f : .1f));

        // make sure to clear the cache later
        ccc.beenHit.Add(target?.gameObject);
        ccc.GetOrAddComponent<RemoveOnTime>().time = 5f;
    }

    public void Reflect()
    {
        if ((target?.CompareTag("Coin") ?? false) && target.TryGetComponent(out TeamCoin c))
        {
            c.ccc = ccc;
            c.power = power + 1;
        }

        if (beam == null)
        {
            // TODO a lotta logic
        }
        else
        {
            agent.tag = "Untagged"; // prevent reflection from hitting the coin
            beam.SetActive(true);
            beam.transform.position = agent.Position;

            if (target)
                beam.transform.LookAt(target);
            else
                beam.transform.forward = Random.onUnitSphere;

            if (beam.TryGetComponent<RevolverBeam>(out var rb))
            {
                rb.damage += power / 4f;
                rb.addedDamage += power / 4f;

                if (doubled && rb.strongAlt && rb.hitAmount < 99) rb.maxHitsPerTarget = ++rb.hitAmount;
                if (isEnemy || isPlayer)
                {
                    var prefix = rb.ultraRicocheter ? "<color=orange>ULTRA</color>" : "";
                    int points = rb.ultraRicocheter ? 100 : 50;
                    if (power > 2) points += (power - 1) * 15;

                    sh.AddPoints(points, "ultrakill.ricoshot", rb.sourceWeapon, null, power - 1, prefix);
                }
            }
        }

        if (IsOwner && doubled && beam == null)
        {
            Hidden = doubled = false;
            Reflect(null);
        }
        else Kill();
    }

    public void Punch()
    {
        if (Hidden)
        {
            if (IsOwner)
            {
                agent.StopAllCoroutines();
                Reflect();
            }
            return;
        }
        if (!coin.enabled) return;

        target = Entities.Coins.FindTarget(this, true, out _, out _);
        Vector3? pos = target
            ? target.position
            : Entities.Coins.Punchcast(out var hit)
                ? hit.point - cc.forward
                : null;

        if (pos == null) Kill();

        Bounce();
        Beam(agent.Position, agent.Position = pos ?? cc.position + cc.forward * 4242f);

        rs.Each(r => { if (r is TrailRenderer t) t.Clear(); });

        if (target)
        {
            Breakable breakable = null;
            var eid = target.GetComponentInChildren<EnemyIdentifierIdentifier>()?.eid ?? (breakable = target.GetComponentInChildren<Breakable>()).interruptEnemy;

            if (!eid.puppet && !eid.blessed) sh.AddPoints(50, "ultrakill.fistfullofdollar", eid: eid);
            if (breakable)
            {
                if (breakable.precisionOnly)
                {
                    sh.AddPoints(100, "ultrakill.interruption", eid: eid);
                    TimeController.Instance.ParryFlash();

                    if (!eid.blessed) eid.Explode(true);
                }
                breakable.Break();
            }
            else
            {
                eid.hitter = "coin";
                eid.DeliverDamage(target.gameObject, (target.position - agent.Position).normalized * 10000f, target.position, power, false, 1f);
            }
        }
        if (power < 5) power++;
    }

    public void Bounce()
    {
        if (Hidden || !coin.enabled) return;

        Locked = false;
        TakeOwnage();

        doubled = false;
        Reset();

        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * 25f, ForceMode.VelocityChange);

        source.Play();
    }

    #endregion
    #region state

    /// <summary> Transform of the coin used by the corresponding vendor. </summary>
    public Transform Transform => agent.transform;

    /// <summary> Resets the state of the coin and restarts a few timers. </summary>
    public void Reset()
    {
        rb.isKinematic = !IsOwner || Hidden;
        Dest(effect);
        agent.StopAllCoroutines();

        if (Hidden) return;

        cs.Each(c => c.enabled = false);
        coin.enabled = false;

        agent.StartCoroutine(CallDelayed(Activate, .1f));
        agent.StartCoroutine(CallDelayed(Double, .35f));
        agent.StartCoroutine(CallDelayed(DoubleEnd, .417f));
        agent.StartCoroutine(CallDelayed(Triple, 1f));
        if (IsOwner)
            agent.StartCoroutine(CallDelayed(() => Kill(), 5f));
    }

    /// <summary> Calls the method after the specified number of seconds. </summary>
    public IEnumerator CallDelayed(Runnable call, float time)
    {
        yield return new WaitForSeconds(time);
        call();
    }

    /// <summary> Activates the coin and colliders. </summary>
    public void Activate()
    {
        cs.Each(c => c.enabled = true);
        coin.enabled = true;
    }

    /// <summary> Switches to the doubled state. </summary>
    public void Double()
    {
        coin.doubled = doubled = true; // punchability is bounded to this field (for some reason)
        Dest(effect);
        effect = Inst(coin.flash, agent.transform);
        effect.GetComponentsInChildren<SpriteRenderer>().Each(r => r.color = team.Color());
    }

    /// <summary> Switches back from the doubled state. </summary>
    public void DoubleEnd() => doubled = false;

    /// <summary> Switches to the tripled state. </summary>
    public void Triple()
    {
        coin.doubled = doubled = true;
        Dest(effect);
        effect = Inst(coin.chargeEffect, agent.transform);
        effect.GetComponentsInChildren<SpriteRenderer  >(true).Each(Dest);
        effect.GetComponentsInChildren<SpriteController>(true).Each(Dest);

        effect.transform.Find("MuzzleFlash/muzzleflash/Particle System").localScale = Vector3.one * .42f;
        effect.transform.Find("MuzzleFlash/muzzleflash"                ).gameObject.SetActive(true);
        var col = effect.GetComponentInChildren<ParticleSystem        >().colorOverLifetime;
        var mat = effect.GetComponentInChildren<ParticleSystemRenderer>().material;
        col.color = new(clear, black);
        mat.color = team.Color();
        mat.mainTexture = null;
    }

    /// <summary> Switches to the quadrupled state. </summary>
    public void Quadruple()
    {
        Dest(effect);
        effect = Inst(coin.enemyFlash, agent.transform);
        effect.GetComponentsInChildren<SpriteRenderer>().Each(r => r.color = team.Color());
        effect.GetComponentsInChildren<Light         >().Each(r => r.color = team.Color());
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(Coin), "Start")]
    [HarmonyPrefix]
    static bool Start(Coin __instance)
    {
        if (__instance) Entities.Coins.Sync(__instance.gameObject);
        return false;
    }

    [HarmonyPatch(typeof(Coin), "OnCollisionEnter")]
    [HarmonyPrefix]
    static bool Death(Coin __instance, Collision collision)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c && LayerMaskDefaults.IsMatchingLayer(collision.gameObject.layer, LMD.Environment))
        {
            if (c.IsOwner) c.Kill();
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.DelayedReflectRevolver))]
    [HarmonyPrefix]
    static bool Reflect(Coin __instance, GameObject beam)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Reflect(beam);
        return false;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.DelayedPunchflection))]
    [HarmonyPrefix]
    static bool Punch(Coin __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Punch();
        return false;
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.Bounce))]
    [HarmonyPrefix]
    static bool Bounce(Coin __instance)
    {
        if (__instance.TryGetComponent(out Agent a) && a.Patron is TeamCoin c) c.Bounce();
        return false;
    }

    #endregion
}
