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
    private void PlaySound(GameObject source)
    {
        if (power > 2) foreach (var sound in source.GetComponents<AudioSource>()) sound.pitch = 1f + (power - 2f) / 5f;
    }

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

    #endregion
}
*/
