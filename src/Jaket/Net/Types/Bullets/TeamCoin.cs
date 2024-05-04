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

    /// <summary> Whether the coin will reflect an incoming beam twice. </summary>
    private bool doubled;
    /// <summary> Whether the coin is in the cooldown phase before shooting to a player. </summary>
    private bool quadrupled;
    /// <summary> Effects indicate the current state of the coin. </summary>
    private GameObject effect;

    /// <summary> Beam that the coin will reflect. </summary>
    private GameObject beam;
    /// <summary> Target that will be hit by reflection. </summary>
    private Transform target;

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

        Coins.Alive.Add(this);
    }

    private void Update()
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
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
        Effect(Coin.enemyFlash, 12f);

        var light = effect.GetComponent<Light>();
        light.color = Team.Color();
        light.intensity = 10f;
    }

    private void Reset()
    {
        Rb.isKinematic = !Dead && (!IsOwner || quadrupled);
        doubled = false;
        Destroy(effect);

        CancelInvoke("Double");
        CancelInvoke("DoubleEnd");
        CancelInvoke("Triple");
        CancelInvoke("NetKill");
        if (Dead || quadrupled) return;

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

    public bool CanHit(TeamCoin other) => other != this && !other.quadrupled;

    public void Reflect(GameObject beam)
    {
        if (beam?.name == "Net") return;
        this.beam = beam;

        target = Coins.FindTarget(this, false, out var isPlayer, out var isEnemy);
        if (isPlayer || isEnemy)
        {
            Quadruple();
            TakeOwnage();
        }
        Invoke("Reflect", isPlayer ? .9f : isEnemy ? .3f : .1f);
    }

    public void Reflect()
    {
        // play the hit sound before killing the coin
        var sounds = Instantiate(Coin.coinHitSound, transform).GetComponents<AudioSource>();
        foreach (var sound in sounds)
        {
            sound.Play();
        }
        NetKill();

        beam ??= Instantiate(Bullets.Prefabs[0], transform.position, Quaternion.identity);
        beam.SetActive(true);
        beam.transform.position = transform.position;

        if (target)
            beam.transform.LookAt(target);
        else
            beam.transform.forward = Random.insideUnitSphere.normalized;

        Rb.AddForce(beam.transform.forward * -42f, ForceMode.VelocityChange);

        beam = null;
        target = null;
    }

    public void Bounce()
    {
        if (!Coin.enabled) return;
        TakeOwnage();
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
    }

    public override void Read(Reader r)
    {
        base.Read(r);
        x.Read(r); y.Read(r); z.Read(r);
    }

    public override void Kill(Reader r)
    {
        base.Kill(r);
        Coin.GetDeleted();
        Coins.Alive.Remove(this);
        Reset();

        mat = GetComponent<Renderer>().material;
        mat.mainTexture = DollAssets.CoinTexture;
        mat.color = Team.Color();
    }

    #endregion
}