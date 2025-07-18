namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;

/// <summary>
/// Doll of a player, remote from network or local from emotes.
/// Responsible for the visual part of the player, i.e. suits and animations.
/// </summary>
public class Doll
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

    /// <summary> Materials of the wings, coin, skateboard and ears. </summary>
    public Material WingMat, CoinMat, SkateMat, EarsMat;
    /// <summary> Trail of the wings. </summary>
    public TrailRenderer WingTrail;
    /// <summary> Light of the wings. </summary>
    public Light WingLight;
    /// <summary> Winch of the hook. </summary>
    public LineRenderer HookWinch;

    public Doll(Runnable onEmote) => OnEmote = onEmote;

    /// <summary> Spawns a preview of the given emote. </summary>
    public static Doll Spawn(Transform parent, Team team, byte emote, byte rps, int hat, int jacket) =>
        Component<Doll>(Inst(ModAssets.DollPreview, parent), doll =>
        {
            doll.transform.localPosition = new(0f, -1.5f);
            doll.transform.localScale *= 2.5f;

            doll.Emote = emote;
            doll.Rps = rps;
            doll.Hat = hat;
            doll.Jacket = jacket;

            doll.ApplyTeam(team);
            doll.ApplySuit();
        });

    /// <summary> Assigns the doll to the given transform. </summary>
    public void Assign(Transform root)
    {
        Animator = (Root = root).GetComponentInChildren<Animator>();
        Transform
            mod = root.Find("Model"),
            rig = root.Find("Model/Metarig");

        Head       = rig.Find("Spine 0/Spine 1/Spine 2#");
        Hand       = rig.Find("Spine 0/Right Shoulder/Right Elbow/Right Wrist");
        Hook       = rig.Find("Hook");
        HookRoot   = rig.Find("Spine 0/Left Shoulder/Left Elbow/Left Wrist/Left Palm");
        Throne     = rig.Find("Throne");
        Coin       = mod.Find("Coin");
        Skateboard = mod.Find("Skateboard");
        Suits      = mod.Find("Suits");

        // parameters of the hand transform are overwritten on weapon swap
        Hand = Create("Weapons Root", Hand).transform;

        WingMat    = mod.Find("Doll").GetComponent<Renderer>().materials[1];
        CoinMat    = Coin.GetComponent<Renderer>().material;
        SkateMat   = Skateboard.GetComponent<Renderer>().material;
        EarsMat    = Suits.Find("Big Ears").GetComponent<Renderer>().material;
        WingTrail  = root.GetComponentInChildren<TrailRenderer>();
        WingLight  = root.GetComponentInChildren<Light>();
        HookWinch  = root.GetComponentInChildren<LineRenderer>(true);

        // update the material and texture of the hook winch to match the original
        if (HookWinch) HookWinch.material = HookArm.Instance.GetComponent<LineRenderer>().material;
    }

    /// <summary> Updates the animation controller state. </summary>
    public void Update()
    {
        Animator.SetBool("walking", Walking);
        Animator.SetBool("sliding", Sliding);
        Animator.SetBool("falling", Falling);
        Animator.SetBool("dashing", Dashing);
        Animator.SetBool("riding", Riding);
        Animator.SetBool("hooking", Hooking);
        Animator.SetBool("shopping", Shopping);

        if (WasFalling != Falling && (WasFalling = Falling)) Animator.SetTrigger("jump");
        if (WasHooking != Hooking)
        {
            Hook.position = HookRoot.position;
            Hook.gameObject.SetActive(WasHooking = Hooking);
        }

        if (LastEmote != Emote)
        {
            Animator.SetTrigger("show-emote");
            Animator.SetInteger("emote", LastEmote = Emote);
            Animator.SetInteger("rps", Rps);
            OnEmote();

            Throne    .gameObject.SetActive(Emote == 0x06);
            Coin      .gameObject.SetActive(Emote == 0x07);
            Skateboard.gameObject.SetActive(Emote == 0x0B);
            if (Emote == 0x08) Head.localEulerAngles = new(-20f, 0f);
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

    #region apply

    /// <summary> Applies the given team to the doll. </summary>
    public void ApplyTeam(Team team)
    {
        WingMat.mainTexture = SkateMat.mainTexture = EarsMat.mainTexture = ModAssets.WingTextures[(int)team];
        CoinMat.color = team.Color();

        if (WingTrail) WingTrail.startColor = team.Color() with { a = .2f };
        if (WingLight) WingLight.color      = team.Color();
    }

    /// <summary> Applies the given item to the doll. </summary>
    public void ApplyItem(byte item)
    {
        Hand.Each(Dest);
        if (item == 0xFF) return;

        Weapons.Instantiate(item, Hand);
        WeaponsOffsets.Apply(item, Hand);
    }

    /// <summary> Applies the saved suit to the doll. </summary>
    public void ApplySuit()
    {
        Suits.Each(s => s.gameObject.SetActive(false));

        int hat = Shop.Entries[Hat].hierarchyId;
        if (hat != -1) Suits.GetChild(hat).gameObject.SetActive(true);

        int jacket = Shop.Entries[Jacket].hierarchyId;
        if (jacket != -1) Suits.GetChild(jacket).gameObject.SetActive(true);

        Hand.GetComponentsInChildren<GunColorGetter>().Each(g =>
        {
            var renderer = g.GetComponent<Renderer>();
            if (CustomColors)
            {
                renderer.materials = g.coloredMaterials;
                renderer.Properties(b =>
                {
                    b.SetColor("_CustomColor1", Color1);
                    b.SetColor("_CustomColor2", Color2);
                    b.SetColor("_CustomColor3", Color3);
                }, true);
            }
            else renderer.materials = g.defaultMaterials;
        });
    }

    #endregion
    #region state

    /// <summary> Writes the animation state into a snapshot. </summary>
    public void WriteAnim(Writer w) => w.Bools(Walking, Sliding, Falling, Slaming, Dashing, Riding, Hooking, Shopping);

    /// <summary> Reads the animation state from a snapshot. </summary>
    public void ReadAnim(Reader r) => r.Bools(out Walking, out Sliding, out Falling, out Slaming, out Dashing, out Riding, out Hooking, out Shopping);

    /// <summary> Reads the style of the suit from a packet. </summary>
    public void ReadSuit(Reader r)
    {
        Hat = r.Int();
        Jacket = r.Int();

        CustomColors = r.Bool();
        if (CustomColors) { Color1 = r.Color(); Color2 = r.Color(); Color3 = r.Color(); }

        ApplySuit();
    }

    #endregion
}
