namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

using static Jaket.UI.Pal;

/// <summary> Representation of a coin that has a team and the corresponding mechanics. </summary>
public class TeamCoin : OwnableEntity
{
    /// <summary> Coin position. </summary>
    private FloatLerp x, y, z;
    /// <summary> Player owning the coin. </summary>
    private EntityProv<RemotePlayer> player = new();

    /// <summary> Coin team can be changed after a punch or hook. </summary>
    public Team Team = (Team)0xFF;
    /// <summary> Material displaying the team, its texture is replaced by white. </summary>
    private Material mat;
    /// <summary> Trail of the coin highlighting the team. </summary>
    private TrailRenderer trail;
    /// <summary> Collision of the coin. Why are there 2 colliders? </summary>
    private Collider[] cols;

    /// <summary> Whether the coin is shot. </summary>
    public bool shot;
    /// <summary> Whether the coin will reflect an incoming beam twice. </summary>
    private bool doubled;
    /// <summary> Whether the coin is in the cooldown phase before shooting to a player or enemy. </summary>
    private bool quadrupled, lastQuadrupled;
    /// <summary> Effects indicate the current state of the coin. </summary>
    private GameObject effect;

    /// <summary> Beam that the coin will reflect. </summary>
    private GameObject beam;
    /// <summary> Target that will be hit by reflection. </summary>
    private Transform target;
    /// <summary> List of objects hit by a chain of ricochets. </summary>
    private CoinChainCache ccc;

    /// <summary> Power of the coin increases after a punch or ricochet. </summary>
    private int power = 2;
    /// <summary> The amount of ricochets increases after punch, so I use only one variable. </summary>
    private int ricochets => power - 2;

    private void Awake()
    {
        Init(_ => Bullets.EType(name), true, coin: true);
        InitTransfer(() =>
        {
            player.Id = Owner;
            if (Team != player.Value?.Team)
            {
                Team = player.Value?.Team ?? Networking.LocalPlayer.Team;
                mat ??= GetComponent<Renderer>().material;
                trail ??= GetComponent<TrailRenderer>();

                mat.mainTexture = DollAssets.CoinTexture;
                mat.color = Team.Color();
                trail.startColor = Team.Color() with { a = .5f };
            }
            Reset();
        });

        x = new(); y = new(); z = new();
        if (IsOwner) OnTransferred();

        Coin.doubled = true; // for some reason, without this, the coin cannot be punched
        Coins.Alive.Add(this);
    }

    private void Start() => ClearTrail(trail, x, y, z);

    private void Update()
    {
        if (IsOwner || Dead) return;

        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
        if (lastQuadrupled != quadrupled && (lastQuadrupled = quadrupled))
        {
            shot = true;
            Reset();
            Quadruple();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (IsOwner && !Dead && (other.gameObject.layer == 8 || other.gameObject.layer == 24))
        {
            var zone = other.transform.GetComponentInParent<GoreZone>();
            if (zone) transform.SetParent(zone.gibZone, true);
            NetKill();
        }
    }

    private void OnDestroy() => Coins.Alive.Remove(this);

    #region state

    private void Activate()
    {
        foreach (var col in cols) col.enabled = true;
        Coin.enabled = true;
    }

    private void Effect(GameObject flash, float size = 20f)
    {
        Destroy(effect);
        effect = Instantiate(flash, transform);

        effect.transform.localScale = Vector3.one * size;
        effect.GetComponentInChildren<SpriteRenderer>(true).color = Team.Color();
    }

    private void Double()
    {
        doubled = true;
        Effect(Coin.flash);
    }

    private void DoubleEnd() => doubled = false;

    private void Triple()
    {
        doubled = true;
        Effect(Coin.chargeEffect);

        effect.transform.GetChild(0).GetChild(0).GetChild(0).localScale = Vector3.one * .42f;
        effect.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        var col = effect.GetComponentInChildren<ParticleSystem>().colorOverLifetime;
        var mat = effect.GetComponentInChildren<ParticleSystemRenderer>().material;
        col.color = new(black, clear);
        mat.color = Team.Color();
        mat.mainTexture = null; // the texture has its own color, which is extremely undesirable
    }

    private void Quadruple()
    {
        quadrupled = true;
        Effect(Coin.enemyFlash, 15f);

        var light = effect.GetComponent<Light>();
        light.color = Team.Color();
        light.intensity = 10f;
    }

    private void Reset()
    {
        Rb.isKinematic = !Dead && (!IsOwner || shot);
        Destroy(effect);

        CancelInvoke("Double");
        CancelInvoke("DoubleEnd");
        CancelInvoke("Triple");
        CancelInvoke("NetKill");
        if (Dead || shot) return;

        foreach (var col in cols ??= GetComponents<Collider>()) col.enabled = false;
        Coin.enabled = false;

        Invoke("Activate", .1f);
        Invoke("Double", .35f);
        Invoke("DoubleEnd", .417f);
        Invoke("Triple", 1f);
        if (IsOwner) Invoke("NetKill", 5f);
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
            Quadruple();
        }
        Invoke("Reflect", (isPlayer ? 1.2f : isEnemy ? .3f : .1f) + offset);

        ccc.beenHit.Add(target?.gameObject);
        if ((target?.CompareTag("Coin") ?? false) && target.TryGetComponent(out TeamCoin c))
        {
            c.ccc = ccc; // :D
            c.power = power + 1;
        }
    }

    public void Reflect()
    {
        // prevent the coin from getting hit by the reflection
        tag = "Untagged";

        // play the sound before killing the coin
        var sounds = Instantiate(Coin.coinHitSound, transform).GetComponents<AudioSource>();
        if (power > 2) foreach (var sound in sounds) sound.pitch = 1f + (power - 2f) / 5f;

        // run the second shot if the player hit the coin in a short timing
        if (doubled && beam == null) // only RV0 PRI can be doubled
            Invoke("DoubleReflect", .1f);
        else
            NetKill();

        beam ??= Instantiate(Bullets.Prefabs[0], transform.position, Quaternion.identity);
        beam.SetActive(true);
        beam.transform.position = transform.position;

        if (target)
            beam.transform.LookAt(target);
        else
            beam.transform.forward = Random.insideUnitSphere.normalized;

        Rb.AddForce(beam.transform.forward * -42f, ForceMode.VelocityChange);

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
        if (!Coin.enabled) return;
        TakeOwnage();

        doubled = false;
        Reset();
    }

    public void Bounce()
    {
        if (quadrupled || !Coin.enabled) return;
        TakeOwnage();

        doubled = false;
        Reset();

        Audio.Play();
        Rb.velocity = Vector3.zero;
        Rb.AddForce(Vector3.up * 25f, ForceMode.VelocityChange);
    }

    #endregion
    #region entity

    public override void Write(Writer w)
    {
        base.Write(w);

        w.Vector(transform.position);
        w.Bool(quadrupled);
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        if (IsOwner) return;

        x.Read(r); y.Read(r); z.Read(r);
        quadrupled = r.Bool();
    }

    public override void Kill(Reader r)
    {
        base.Kill(r);
        Reset();

        Coin.GetDeleted();
        Coins.Alive.Remove(this);

        mat = GetComponent<Renderer>().material;
        mat.mainTexture = DollAssets.CoinTexture;
        mat.color = Team.Color();
    }

    #endregion
}
