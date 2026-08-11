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
    global::Enemy enemy;
    Animator animator;
    TrailRenderer wingTrail;
    Light wingLight;
    LineRenderer hookWinch;
    Material wings, coin, skate, ears;

    /// <summary> Present value of the state machine.  </summary>
    public bool Walking, Sliding, Falling, Slaming, Riding, Hooking, Shopping;
    /// <summary> Identifier of the played animation. </summary>
    public byte Emote, Rps, LastEmote = 0xFF;

    /// <summary> Team required for versus mechanics. </summary>
    public Team Team = Team.None;
    /// <summary> Identifier of the displayed weapon. </summary>
    public EntityType Weapon = EntityType.None;

    /// <summary> Various parts of the doll. </summary>
    public Transform Root, Head, Hand, Reel, Hook, Chair, Coin, Skate, Suits;
    /// <summary> Various particles/effects. </summary>
    public Transform SlidParticle, SlamParticle;

    /// <summary> Position to hold items in. </summary>
    public Vector3 HoldPosition => Hooking ? Hook.position : Reel.position;
    /// <summary> Rotation of the doll head. </summary>
    public float HeadAngle { set => animator.SetFloat("head-angle", (90f + value) / 180f); }

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

            wings.mainTexture = skate.mainTexture = ears.mainTexture = ModAssets.WingTextures[(byte)team];
            coin.color = team.Color();

            wingTrail.startColor = team.Color() with { a = .2f };
            wingLight.     color = team.Color();
        });

        if (Weapon != weap) Events.Post(() =>
        {
            Weapon = weap;

            Hand.Each(Dest);
            if (weap == EntityType.None) return;

            Entities.Weapons.Make(weap, parent: Hand);
            Transformations.Apply(weap, target: Hand);
        });

        var hat = Shop.Entries[r.Byte()].hierarchy;
        var jkt = Shop.Entries[r.Byte()].hierarchy;

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

        agent.Get(out enemy);
        agent.Get(out animator);
        agent.Get(out wingTrail, true);
        agent.Get(out wingLight, true);
        agent.Get(out hookWinch, true);

        agent.Get(out Renderer rw, path: "Doll/Models/Doll"      ); wings = rw.materials[1];
        agent.Get(out Renderer rc, path: "Doll/Models/Coin"      ); coin  = rc.materials[0];
        agent.Get(out Renderer rs, path: "Doll/Models/Skateboard"); skate = rs.materials[0];
        agent.Get(out Renderer re, path: "Doll/Suits/Big Ears"   ); ears  = re.materials[0];

        agent.Add<PortalAwareRenderer>(out _);
        agent.Add<PortalAwareLight>(out _, true, "Doll/Metarig/Hips/Spine 0/Trail");

        Hand = Tools.Tools.Create("Weapons Root", Hand).transform;
        hookWinch?.material = HookArm.Instance.GetComponent<LineRenderer>().material;
    }

    public override void Update(float delta)
    {
        if (Falling && !animator.GetBool("falling")) animator.SetTrigger("jump");

        animator.SetBool("walking", Walking);
        animator.SetBool("sliding", Sliding);
        animator.SetBool("falling", Falling);
        animator.SetBool("riding", Riding);
        animator.SetBool("hooking", Hooking);
        animator.SetBool("shopping", Shopping);

        if (LastEmote != Emote)
        {
            animator.SetTrigger("show-emote");
            animator.SetInteger("emote", LastEmote = Emote);
            animator.SetInteger("rps", Rps);

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
        else if (!Sliding && SlidParticle != null) Dest(SlidParticle);

        if (Slaming && SlamParticle == null)
        {
            SlamParticle = Inst(NewMovement.Instance.fallParticle, Root).transform;
            SlamParticle.localPosition    = new(  0f,   6f,   0f);
            SlamParticle.localEulerAngles = new( 90f,   0f,   0f);
            SlamParticle.localScale       = new(1.2f,  .6f,   1f);
        }
        else if (!Slaming && SlamParticle != null) Dest(SlamParticle);

        Hook.gameObject.SetActive(Hooking);
        Hook.LookAt(Reel);
        Hook.Rotate(Vector3.up * 180f, Space.Self);
        hookWinch.SetPosition(0, Reel.position);
        hookWinch.SetPosition(1, Hook.position);
    }

    public override void Damage(Reader r) { }

    public override void Killed(Reader r, int left)
    {
        enemy.GoLimp();
        if (wingTrail   ) Dest(wingTrail   );
        if (wingLight   ) Dest(wingLight   );
        if (SlidParticle) Dest(SlidParticle);
        if (SlamParticle) Dest(SlamParticle);
    }

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

    /// <summary> Clears the trail of the model's wings. </summary>
    public void Clear() { if (wingTrail) wingTrail.Clear(); }

    #endregion
}
