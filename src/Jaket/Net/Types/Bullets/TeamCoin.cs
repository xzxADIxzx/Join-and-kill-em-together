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

    private void Awake()
    {
        Init(_ => Bullets.EType(name), true);
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
        });

        x = new(); y = new(); z = new();

        if (IsOwner) OnTransferred();
        foreach (var col in GetComponents<Collider>()) col.enabled = true;
    }

    private void Update()
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Get(LastUpdate), z.Get(LastUpdate));
    }

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

    #endregion
}
