namespace Jaket.Net.Types;

using System;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.IO;
using Jaket.UI;

/// <summary>
/// Doll of a player, remote from network or local from emote.
/// Responsible for the visual part of the player, i.e. suits and animations.
/// </summary>
public class Doll : MonoBehaviour
{
    /// <summary> Component rendering animations of the doll. </summary>
    public Animator Animator;
    /// <summary> Animator states that affect which animation will be played. </summary>
    public bool Walking, Sliding, Falling, InAir, WasInAir, Dashing, WasDashing, Riding, WasRiding, Hooking, WasHooking, Shopping, WasShopping;

    /// <summary> Emote that plays at the moment. </summary>
    public byte Emote, LastEmote = 0xFF, Rps;
    /// <summary> Event triggered after the start of emote. </summary>
    public Action OnEmoteStart = () => { };

    /// <summary> Hat and jacket that the doll wears. </summary>
    public int Hat, Jacket;
    /// <summary> Whether the player uses custom weapon colors. </summary>
    public bool CustomColors;
    /// <summary> Custom weapon colors themselves. </summary>
    public Color32 Color1, Color2, Color3;

    /// <summary> Transforms of different parts of the body. </summary>
    public Transform Head, Hand, Hook, HookRoot, Throne, Coin, Skateboard, Suits;
    /// <summary> Slide and fall particles transforms. </summary>
    public Transform SlideParticle, FallParticle;
    /// <summary> Position in which the doll holds an item. </summary>
    public Vector3 HoldPosition => Hooking ? Hook.position : HookRoot.position;

    /// <summary> Materials of the wings, coin, skateboard and big ears. </summary>
    public Material WingMat, CoinMat, SkateMat, EarsMat;
    /// <summary> Trail of the wings. </summary>
    public TrailRenderer WingTrail;
    /// <summary> Light of the wings. </summary>
    public Light WingLight;
    /// <summary> Winch of the hook. </summary>
    public LineRenderer HookWinch;

    /// <summary> Spawns a preview of the given emote. </summary>
    public static Doll Spawn(Transform parent, Team team, int hat, int jacket, byte emote, byte rps) =>
        UIB.Component<Doll>(Instantiate(ModAssets.Preview, parent), doll =>
        {
            doll.transform.localPosition = new(0f, -1.5f);
            doll.transform.localScale = Vector3.one * 2.18f;

            doll.Hat = hat;
            doll.Jacket = jacket;
            doll.Emote = emote;
            doll.Rps = rps;

            doll.ApplyTeam(team);
            doll.ApplySuit();
        });

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        Transform V3 = transform.Find("V3"), rig = V3.Find("Metarig");

        Head = rig.Find("Spine 0/Spine 1/Spine 2");
        Hand = rig.Find("Spine 0/Right Shoulder/Right Elbow/Right Wrist");
        Hand = Tools.Create("Weapons Root", Hand).transform;
        Hook = rig.Find("Hook");
        HookRoot = rig.Find("Spine 0/Left Shoulder/Left Elbow/Left Wrist/Left Palm");
        Throne = rig.Find("Throne");
        Coin = V3.Find("Coin");
        Skateboard = V3.Find("Skateboard");
        Suits = V3.Find("Suits");

        WingMat = V3.Find("V3").GetComponent<Renderer>().materials[1];
        CoinMat = Coin.GetComponent<Renderer>().material;
        SkateMat = Skateboard.GetComponent<Renderer>().material;
        EarsMat = Suits.Find("Big Ears").GetComponent<Renderer>().material;
        WingTrail = GetComponentInChildren<TrailRenderer>();
        WingLight = GetComponentInChildren<Light>();
        HookWinch = GetComponentInChildren<LineRenderer>(true);
    }

    private void Update() => Stats.MTE(() =>
    {
        if (Animator == null) return;

        Animator.SetBool("walking", Walking);
        Animator.SetBool("sliding", Sliding);
        Animator.SetBool("in-air", InAir);
        Animator.SetBool("dashing", Dashing);
        Animator.SetBool("riding", Riding);
        Animator.SetBool("hooking", Hooking);
        Animator.SetBool("shopping", Shopping);

        if (WasInAir != InAir && (WasInAir = InAir)) Animator.SetTrigger("jump");
        if (WasRiding != Riding && (WasRiding = Riding)) Animator.SetTrigger("ride");
        if (WasDashing != Dashing && (WasDashing = Dashing)) Animator.SetTrigger("dash");
        if (WasHooking != Hooking)
        {
            if (WasHooking = Hooking) Animator.SetTrigger("hook");

            Hook.position = HookRoot.position;
            Hook.gameObject.SetActive(Hooking);
        }
        if (WasShopping != Shopping && (WasShopping = Shopping)) Animator.SetTrigger("open-shop");

        if (LastEmote != Emote)
        {
            Animator.SetTrigger("show-emoji");
            Animator.SetInteger("emoji", LastEmote = Emote);
            Animator.SetInteger("rps", Rps);

            Throne.gameObject.SetActive(Emote == 6);
            Coin.gameObject.SetActive(Emote == 7);
            Skateboard.gameObject.SetActive(Emote == 11);
            if (Emote == 8) Head.localEulerAngles = new(-20f, 0f);

            OnEmoteStart();
        }

        if (Sliding && SlideParticle == null)
        {
            SlideParticle = Instantiate(NewMovement.Instance.slideParticle, transform).transform;
            SlideParticle.localPosition = new(0f, 0f, 3.5f);
            SlideParticle.localEulerAngles = new(0f, 180f, 0f);
            SlideParticle.localScale = new(1.5f, 1f, .8f);
        }
        else if (!Sliding && SlideParticle != null) Destroy(SlideParticle.gameObject);

        if (Falling && FallParticle == null)
        {
            FallParticle = Instantiate(NewMovement.Instance.fallParticle, transform).transform;
            FallParticle.localPosition = new(0f, 6f, 0f);
            FallParticle.localEulerAngles = new(90f, 0f, 0f);
            FallParticle.localScale = new(1.2f, .6f, 1f);
        }
        else if (!Falling && FallParticle != null) Destroy(FallParticle.gameObject);
    });

    #region apply

    public void ApplyTeam(Team team)
    {
        WingMat.mainTexture = SkateMat.mainTexture = EarsMat.mainTexture = ModAssets.WingTextures[(int)team];
        CoinMat.color = team.Color();

        if (WingTrail) WingTrail.startColor = team.Color() with { a = .5f };
        if (WingLight) WingLight.color = team.Color();
    }

    public void ApplySuit()
    {
        foreach (Transform suit in Suits) suit.gameObject.SetActive(false);

        int hat = Shop.Entries[Hat].hierarchyId;
        if (hat != -1) Suits.GetChild(hat).gameObject.SetActive(true);

        int jacket = Shop.Entries[Jacket].hierarchyId;
        if (jacket != -1) Suits.GetChild(jacket).gameObject.SetActive(true);

        foreach (var getter in Hand.GetComponentsInChildren<GunColorGetter>())
        {
            var renderer = getter.GetComponent<Renderer>();
            if (CustomColors)
            {
                renderer.materials = getter.coloredMaterials;
                UIB.Properties(renderer, block =>
                {
                    block.SetColor("_CustomColor1", Color1);
                    block.SetColor("_CustomColor2", Color2);
                    block.SetColor("_CustomColor3", Color3);
                }, true);
            }
            else renderer.materials = getter.defaultMaterials;
        }
    }

    #endregion
    #region entity

    public void WriteAnim(Writer w) => w.Bools(Walking, Sliding, Falling, InAir, Dashing, Riding, Hooking, Shopping);

    public void ReadAnim(Reader r) => r.Bools(out Walking, out Sliding, out Falling, out InAir, out Dashing, out Riding, out Hooking, out Shopping);

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
