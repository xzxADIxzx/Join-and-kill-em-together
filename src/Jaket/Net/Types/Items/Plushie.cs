namespace Jaket.Net.Types;

using HarmonyLib;
using System.Collections;
using UnityEngine;

using Jaket.Assets;
using Jaket.Content;

/// <summary> Tangible entity of any plushie type. </summary>
public class Plushie : Item
{
    Agent agent;

    public Plushie(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => new(30f, 0f, 180f);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.GetComponent<ItemIdentifier>().onPickUp.onActivate.AddListener(() =>
        {
            if (Type == EntityType.xzxADIxzx) agent.StartCoroutine(ShakeYourHead(42));
            if (Type == EntityType.Sowler) agent.StartCoroutine(Hoot());
        });
        agent.transform.Each(c => c.gameObject.layer = 22); // the plushie of lizard has an issue with layers

        Imdt(agent.transform.Find("GlassesHitbox")?.gameObject);
    }

    #endregion
    #region other

    /// <summary> Special feature of the plushie of xzxADIxzx. </summary>
    public IEnumerator ShakeYourHead(int shakes)
    {
        var head = agent.transform.Find("Model/Head");

        while (shakes-- > 1)
        {
            head.localEulerAngles = new(90f * Random.Range(0, 3), 90f * Random.Range(0, 3), 90f * Random.Range(0, 3));
            yield return new WaitForSeconds(.4f / shakes);
        }

        head.localEulerAngles = new(270f, Random.value < .042f ? 45f : 0f, 0f);
    }

    /// <summary> Special feature of the plushie of OwlNotSowler. </summary>
    public IEnumerator Hoot()
    {
        yield return null; // TODO play different owl sounds from time to time
    }

    #endregion
    #region harmony

    [HarmonyPatch(typeof(ItemTrigger), "OnTriggerEnter")]
    [HarmonyPrefix]
    static bool Trash(ItemTrigger __instance, Collider other)
    {
        var agent = other.GetComponentInParent<Agent>();
        if (agent && Scene == "CreditsMuseum2")
        {
            if (agent.Patron is Item i && i.IsOwner && __instance.targetType == ItemType.CustomKey1)
            {
                if (i.Type == EntityType.xzxADIxzx) Trash(__instance);
                if (i.Type == EntityType.Sowler) Trash(other);
                if (i.Type != EntityType.Sowler) i.Kill(1, w => w.Bool(true));
            }
            return false;
        }
        else return true;
    }

    static void Trash(ItemTrigger trigger)
    {
        var black = Inst(ObjFind("SORT ME").transform.Find("OBJECT Activator Gianni world enemies/time of day changer").gameObject);
        var white = Inst(ObjFind("SORT ME").transform.Find("time of day reverser").gameObject);
        var music = ObjFind("Music");

        var gif = trigger.onEvent.toActivateObjects[0];
        if (gif.TryGetComponent(out RandomPitch rnd) && gif.TryGetComponent(out ObjectActivator act))
        {
            rnd.defaultPitch = .2f;
            rnd.pitchVariation = .01f;
            act.delay = 8f;

            black.SetActive(true);
            white.SetActive(false);
            music.SetActive(false);

            act.events.onActivate.AddListener(() =>
            {
                rnd.defaultPitch = 1f;
                rnd.pitchVariation = .1f;
                act.delay = 1f;

                black.SetActive(false);
                white.SetActive(true);
                music.SetActive(true);
            });
        }
    }

    static void Trash(Collider col)
    {
        var mov = NewMovement.Instance.transform;
        var cam = CameraController.Instance.transform;
        var owl = col.transform.parent.parent;

        owl.position = cam.position - mov.forward * 6f - Vector3.up;
        owl.LookAt(cam);
        col.attachedRigidbody.isKinematic = true;

        GameAssets.Sound("Voices/Gabriel/gab_Intro1d.ogg", c =>
        {
            var src = Component<AudioSource>(owl.gameObject, src =>
            {
                src.clip = c;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.Play();
            });
            var act = Component<ObjectActivator>(owl.gameObject, act =>
            {
                act.ActivateDelayed(5f);
                act.events = new() { onActivate = new() };
                act.events.onActivate.AddListener(() => { Dest(src); Dest(act); });
            });
        });
    }

    #endregion
}
