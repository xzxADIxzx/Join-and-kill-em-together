namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary> Representation of a coin that has a team and the corresponding mechanics. </summary>
public class TeamCoin : OwnableEntity
{
    /// <summary> Coin position. </summary>
    private FloatLerp x, y, z;
    /// <summary> Player owning the coin. </summary>
    private EntityProv<RemotePlayer> player = new();

    /// <summary> Coin team can be changed after a punch or hook. </summary>
    private Team team = (Team)0xFF;
    /// <summary> Material displaying the team, its texture is replaced by white. </summary>
    private Material mat;
    /// <summary> Trail of the coin highlighting the team. </summary>
    private TrailRenderer trail;
    /// <summary> Collision of the coin. Why are there 2 colliders? </summary>
    private Collider[] cols;

    private void Awake()
    {
        Init(_ => Bullets.EType(name), true, coin: true);
        InitTransfer(() =>
        {
            player.Id = Owner;
            if (team != player.Value?.Team)
            {
                team = player.Value?.Team ?? Networking.LocalPlayer.Team;
                mat ??= GetComponent<Renderer>().material;
                trail ??= GetComponent<TrailRenderer>();

                mat.mainTexture = DollAssets.CoinTexture;
                mat.color = team.Color();
                trail.startColor = team.Color() with { a = .5f };
            }
            Reset();
        });

        x = new(); y = new(); z = new();
        if (IsOwner) OnTransferred();
    }

    private void Update()
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
    }

    private void OnCollisionEnter(Collision other)
    {
        if (IsOwner && (other.gameObject.layer == 8 || other.gameObject.layer == 24))
        {
            var zone = other.transform.GetComponentInParent<GoreZone>();
            if (zone) transform.SetParent(zone.gibZone, true);
            NetKill();
        }
    }

    #region state

    private void Activate()
    {
        foreach (var col in cols) col.enabled = true;
        Coin.enabled = true;
    }

    private void Reset()
    {
        CancelInvoke("NetKill");
        if (Dead) return;

        foreach (var col in cols ??= GetComponents<Collider>()) col.enabled = false;
        Coin.enabled = false;

        Invoke("Activate", 0.1f);
        if (IsOwner) Invoke("NetKill", 5f);
    }

    #endregion
    #region interactions

    public void Reflect(GameObject beam)
    {

    }

    public void Punch()
    {

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
        Reset();

        mat = GetComponent<Renderer>().material;
        mat.mainTexture = DollAssets.CoinTexture;
        mat.color = team.Color();
    }

    #endregion
}
