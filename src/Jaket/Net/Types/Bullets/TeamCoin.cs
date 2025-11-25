/*
namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

using static Jaket.UI.Lib.Pal;

/// <summary> Representation of a coin that has a team and the corresponding mechanics. </summary>
public class TeamCoin : OwnableEntity
{
    static Transform cc => CameraController.Instance.transform;
    static StyleHUD sh => StyleHUD.Instance;

    /// <summary> Target that will be hit by reflection. </summary>
    private Transform target;
    /// <summary> List of objects hit by a chain of ricochets. </summary>
    private CoinChainCache ccc;

    private void PlaySound(GameObject source)
    {
        if (power > 2) foreach (var sound in source.GetComponents<AudioSource>()) sound.pitch = 1f + (power - 2f) / 5f;
    }

    #region state

    private void Effect(GameObject flash, float size)
    {
        Dest(effect);
        effect = Inst(flash, transform);

        effect.transform.localScale = Vector3.one * size;
        effect.GetComponentInChildren<SpriteRenderer>(true).color = Team.Color();
    }

    private void Double()
    {
        coin.doubled = true; // for some reason, without this, the coin cannot be punched

        doubled = true;
        Effect(coin.flash, 20f);
    }

    private void DoubleEnd() => doubled = false;

    private void Triple()
    {
        doubled = true;
        Effect(coin.chargeEffect, 12f);

        effect.transform.GetChild(0).GetChild(0).GetChild(0).localScale = Vector3.one * .42f;
        effect.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        var col = effect.GetComponentInChildren<ParticleSystem>().colorOverLifetime;
        var mat = effect.GetComponentInChildren<ParticleSystemRenderer>().material;
        col.color = new(black, clear);
        mat.color = Team.Color();
        mat.mainTexture = null; // the texture has its own color, which is extremely undesirable
    }

    private void Quadruple(bool silent = false)
    {
        quadrupled = true;
        Effect(coin.enemyFlash, 15f);

        var light = effect.GetComponent<Light>();
        light.color = Team.Color();
        light.intensity = 10f;
        if (silent) Dest(effect.GetComponent<AudioSource>());
    }

    #endregion
    #region interactions

    public void Reflect(GameObject beam, float offset = 0f)
    {
        if (beam?.name == "Net(Clone)") return;
        this.beam = beam;

        if (quadrupled)
        {
            if (!Team.Ally())
            {
                // achievement unlocked: return to the sender
                target = player.Value.Doll.Head.transform;

                CancelInvoke("Reflect");
                Reflect();
            }
            return;
        }

        shot = true;
        Reset();

        target = Coins.FindTarget(this, false, out var isPlayer, out var isEnemy, ccc ??= gameObject.AddComponent<CoinChainCache>());
        if (isPlayer || isEnemy)
        {
            TakeOwnage();
            Quadruple(isEnemy);
        }
        Invoke("Reflect", (isPlayer ? 1.2f : isEnemy ? .3f : .1f) + offset);

        ccc.beenHit.Add(target?.gameObject);
        if ((target?.CompareTag("Coin") ?? false) && target.TryGetComponent(out TeamCoin c))
        {
            c.ccc = ccc; // :D
            c.power = ++power;
        }
    }

    public void Reflect()
    {
        // prevent the coin from getting hit by the reflection
        tag = "Untagged";

        // play the sound before killing the coin
        PlaySound(Inst(coin.coinHitSound, transform));

        var rvp = beam == null; // only RV1 PRI can be doubled
        if (rvp && doubled)
            Invoke("DoubleReflect", .1f); // run the second shot if the player hit the coin in a short timing
        else
            NetKill();

        if (rvp) Coins.PaintBeam(beam = Inst(Bullets.Prefabs[0], Vector3.zero), Team);
        beam.SetActive(true);
        beam.transform.position = transform.position;

        if (target)
            beam.transform.LookAt(target);
        else
            beam.transform.forward = Random.insideUnitSphere.normalized;

        Rb.AddForce(beam.transform.forward * -42f, ForceMode.VelocityChange);

        if (beam.TryGetComponent<RevolverBeam>(out var rb))
        {
            if (rvp)
                rb.damage = power;
            else
            {
                rb.damage += power / 4f - rb.addedDamage;
                rb.addedDamage = power / 4f;
            }

            if (doubled && rb.strongAlt && rb.hitAmount < 99) rb.maxHitsPerTarget = ++rb.hitAmount;

            if (quadrupled)
            {
                var prefix = rb.ultraRicocheter ? "<color=orange>ULTRA</color>" : "";
                int points = rb.ultraRicocheter ? 100 : 50;
                if (power > 2) points += (power - 1) * 15;

                sh.AddPoints(points, "ultrakill.ricoshot", rb.sourceWeapon, null, power - 1, prefix);
            }
        }

        doubled = quadrupled = false; // before the second shot, the coin can flash again
        beam = null;
        target = null;
    }

    public void DoubleReflect() => Reflect(null, -.1f);

    public void Punch()
    {
        if (quadrupled)
        {
            if (IsOwner)
            {
                CancelInvoke("Reflect");
                Reflect();
            }
            return;
        }
        if (!coin.enabled) return;
        Bounce();

        target = Coins.FindTarget(this, true, out _, out _, null);
        Vector3? pos = target
            ? target.position
            : Coins.Punchcast(out var hit)
                ? hit.point - cc.forward
                : null;

        // make the coin unavailable for future use
        if (pos == null) shot = true;

        var beam = Inst(coin.refBeam, transform.position).GetComponent<LineRenderer>();
        PlaySound(beam.gameObject);
        trail.Clear();

        beam.startColor = beam.endColor = Team.Color();
        beam.SetPosition(0, transform.position);
        beam.SetPosition(1, transform.position = pos ?? cc.position + cc.forward * 4200f);

        // deal a damage if the coin hit an enemy
        if (target)
        {
            Breakable breakable = null;
            var eid = target.GetComponentInChildren<EnemyIdentifierIdentifier>()?.eid;
            eid ??= (breakable = target.GetComponentInChildren<Breakable>()).interruptEnemy;

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
                if (!eid.hitterWeapons.Contains("coin")) eid.hitterWeapons.Add("coin");

                eid.DeliverDamage(target.gameObject, (target.position - transform.position).normalized * 10000f, target.position, power, false, 1f);
            }
        }
        if (power < 5) power++;
    }

    public void Bounce()
    {
        if (quadrupled || !coin.enabled) return;
        TakeOwnage();

        doubled = false;
        Reset();

        audio.Play();
        Rb.velocity = Vector3.zero;
        Rb.AddForce(Vector3.up * 25f, ForceMode.VelocityChange);
    }

    #endregion
}
*/
