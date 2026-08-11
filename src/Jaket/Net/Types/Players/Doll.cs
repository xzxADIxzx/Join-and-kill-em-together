namespace Jaket.Net.Types;

using ULTRAKILL.Portal;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Input;
using Jaket.IO;

/// <summary>
/// Doll of a player, remote from network or local from emotes.
/// Responsible for the visual part of the player.
/// </summary>
public class Doll : Entity
{
    /// <summary> Animation controller of the doll. </summary>
    public Animator Animator;
    /// <summary> Animation booleans that affect the state machine. </summary>
    public bool Walking, Sliding, Falling, Slaming, Dashing, Riding, Hooking, Shopping, WasFalling, WasHooking;

    /// <summary> Emote that is playing at the moment. </summary>
    public byte Emote, LastEmote = 0xFF, Rps;
    /// <summary> Event that is triggered when emote changes. </summary>
    public Runnable OnEmote;

    /// <summary> Hat and jacket that are worn by the doll. </summary>
    public int Hat, Jacket;
    /// <summary> Whether custom weapon colors are used. </summary>
    public bool CustomColors;
    /// <summary> Custom weapon colors themselves. </summary>
    public Color32 Color1, Color2, Color3;

    /// <summary> Transforms of different parts of the doll. </summary>
    public Transform Root, Head, Hand, Hook, HookRoot, Throne, Coin, Skateboard, Suits;
    /// <summary> Sliding and slaming particles transforms. </summary>
    public Transform SlidParticle, SlamParticle;
    /// <summary> Position in which the doll holds an item. </summary>
    public Vector3 HoldPosition => Hooking ? Hook.position : HookRoot.position;
    /// <summary> Angle of the head rotation relative to the respective bone. </summary>
    public float HeadAngle { set => Animator.SetFloat("head-angle", (90f + value) / 180f); }

    /// <summary> Materials of the wings, coin, skateboard and ears. </summary>
    public Material WingMat, CoinMat, SkateMat, EarsMat;
    /// <summary> Trail of the wings. </summary>
    public TrailRenderer WingTrail;
    /// <summary> Light of the wings. </summary>
    public Light WingLight;
    /// <summary> Winch of the hook. </summary>
    public LineRenderer HookWinch;

    public Doll() : base(AccId, EntityType.None) { }

    #region snapshot

    public override int BufferSize => 17;

    public override void Write(Writer w)
    {
        Renderer r = null;
        var weapon = GunControl.Instance.currentWeapon;
        var custom = (weapon?.GetComponentInChildren<GunColorGetter>()?.TryGetComponent(out r) ?? false) && r.material.name.Contains("Custom");

        w.Enum(Networking.LocalPlayer.Team);
        w.Enum(Entities.Weapons.Type(weapon));

        w.Byte(Shop.SelectedHat);
        w.Byte(Shop.SelectedJacket);

        w.Bool(custom);
        if (custom) r.Properties(b =>
        {
            w.Color(b.GetColor("_CustomColor1"));
            w.Color(b.GetColor("_CustomColor2"));
            w.Color(b.GetColor("_CustomColor3"));
        });
    }

    public override void Read(Reader r)
    {
        var team = r.Team();
        var weap = r.EntityType();

        if (Team != team) Events.Post(() =>
        {
            Team = team;
            Events.OnTeamChange.Fire();

            WingMat.mainTexture = SkateMat.mainTexture = EarsMat.mainTexture = ModAssets.WingTextures[(byte)team];
            CoinMat.color = team.Color();

            WingTrail.startColor = team.Color() with { a = .2f };
            WingLight.     color = team.Color();
        });

        if (Weapon != weap) Events.Post(() =>
        {
            Weapon = weap;

            Hand.Each(Dest);
            if (weap == EntityType.None) return;

            Entities.Weapons.Make(weap, parent: Hand);
            Transformations.Apply(weap, target: Hand);
        });

        var hat = Shop.Entries[r.Byte()].hierarchyId;
        var jkt = Shop.Entries[r.Byte()].hierarchyId;

        Events.Post(() =>
        {
            Suits.Each(s => s.gameObject.SetActive(false));

            if (hat != -1) Suits.GetChild(hat).gameObject.SetActive(true);
            if (jkt != -1) Suits.GetChild(jkt).gameObject.SetActive(true);
        });

        var custom = r.Bool();
        var color1 = r.Color();
        var color2 = r.Color();
        var color3 = r.Color();

        Events.Post(() =>
        {
            Hand.GetComponentsInChildren<GunColorGetter>().Each(g => g.Component<Renderer>(r =>
            {
                if (custom)
                {
                    r.materials = g.coloredMaterials;
                    r.Properties(b =>
                    {
                        b.SetColor("_CustomColor1", color1);
                        b.SetColor("_CustomColor2", color2);
                        b.SetColor("_CustomColor3", color3);
                    }, true);
                }
                else r.materials = g.defaultMaterials;
            }, true));
        });
    }

    #endregion
    #region logic

    public override void Create() { }

    public override void Assign(Agent agent)
    {
        agent.Get(out Root);
        agent.Get(out Head,  path: "Doll/Metarig/Hips/Spine 0/Spine 1/Spine 2");
        agent.Get(out Hand,  path: "Doll/Metarig/Hips/Spine 0/Right Shoulder/Right Elbow/Right Wrist/Right Palm");
        agent.Get(out Reel,  path: "Doll/Metarig/Hips/Spine 0/Left Shoulder/Left Elbow/Left Wrist/Left Palm");
        agent.Get(out Hook,  path: "Doll/Models/Hook");
        agent.Get(out Chair, path: "Doll/Models/Throne");
        agent.Get(out Coin,  path: "Doll/Models/Coin");
        agent.Get(out Skate, path: "Doll/Models/Skateboard");
        agent.Get(out Suits, path: "Doll/Suits");

        agent.Get(out Animator);
        agent.Get(out WingTrail, true);
        agent.Get(out WingLight, true);
        agent.Get(out HookWinch, true);

        agent.Get(out Renderer rw, path: "Doll/Models/Doll"      ); WingMat  = rw.materials[1];
        agent.Get(out Renderer rc, path: "Doll/Models/Coin"      ); CoinMat  = rc.materials[0];
        agent.Get(out Renderer rs, path: "Doll/Models/Skateboard"); SkateMat = rs.materials[0];
        agent.Get(out Renderer re, path: "Doll/Suits/Big Ears"   ); EarsMat  = re.materials[0];

        agent.Add<PortalAwareRenderer>(out _);
        agent.Add<PortalAwareLight>(out _, path: "Doll/Metarig/Hips/Spine 0/Trail");

        Hand = Tools.Tools.Create("Weapons Root", Hand).transform;
        HookWinch?.material = HookArm.Instance.GetComponent<LineRenderer>().material;
    }

    public override void Update(float delta)
    {
        if (Falling && !Animator.GetBool("falling")) Animator.SetTrigger("jump");

        Animator.SetBool("walking", Walking);
        Animator.SetBool("sliding", Sliding);
        Animator.SetBool("falling", Falling);
        Animator.SetBool("riding", Riding);
        Animator.SetBool("hooking", Hooking);
        Animator.SetBool("shopping", Shopping);

        if (LastEmote != Emote)
        {
            Animator.SetTrigger("show-emote");
            Animator.SetInteger("emote", LastEmote = Emote);
            Animator.SetInteger("rps", Rps);

            Hand .gameObject.SetActive(Emote == 0xFF);
            Chair.gameObject.SetActive(Emote == 0x06);
            Coin .gameObject.SetActive(Emote == 0x07);
            Skate.gameObject.SetActive(Emote == 0x0B);
        }

        if (Sliding && SlidParticle == null)
        {
            SlidParticle = Inst(NewMovement.Instance.slideParticle, Root).transform;
            SlidParticle.localPosition    = new(  0f,   0f, 3.5f);
            SlidParticle.localEulerAngles = new(  0f, 180f,   0f);
            SlidParticle.localScale       = new(1.5f,   1f,  .8f);
        }
        else if (!Sliding && SlidParticle != null) Dest(SlidParticle.gameObject);

        if (Slaming && SlamParticle == null)
        {
            SlamParticle = Inst(NewMovement.Instance.fallParticle, Root).transform;
            SlamParticle.localPosition    = new(  0f,   6f,   0f);
            SlamParticle.localEulerAngles = new( 90f,   0f,   0f);
            SlamParticle.localScale       = new(1.2f,  .6f,   1f);
        }
        else if (!Slaming && SlamParticle != null) Dest(SlamParticle.gameObject);
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left) { }

    #endregion
    #region other

    /// <summary> Creates a preview of the local player. </summary>
    public static void Preview() => Component<Agent>(Inst(ModAssets.DollPreview, NewMovement.Instance.transform), a =>
    {
        Doll doll = new()
        {
            Emote = Emotes.Current,
            Rps = Emotes.Rps
        };
        doll.Assign(a);
        doll.Write(new(Pointers.Allocated));
        doll.Read(new(Pointers.Allocated));
        doll.Update(0);
    }
    ).transform.localPosition = Vector3.down * 1.5f;

    #endregion
}
