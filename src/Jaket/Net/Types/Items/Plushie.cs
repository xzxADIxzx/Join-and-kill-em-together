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
    ItemIdentifier itemId;

    public Plushie(uint id, EntityType type) : base(id, type) { }

    #region logic

    public override Vector3 HoldRotation => new(30f, 0f, 180f);

    public override void Assign(Agent agent)
    {
        base.Assign(this.agent = agent);

        agent.Get(out itemId);
        agent.Rem("GlassesHitbox");

        itemId.onPickUp.onActivate.AddListener(() =>
        {
            agent.Rem("RageEffect(Clone)", true);
            agent.StopAllCoroutines();

            if (Type == EntityType.xzxADIxzx) agent.StartCoroutine(ShakeYourHead(42));
            if (Type == EntityType.Sowler   ) agent.StartCoroutine(Hoot());
        });
        agent.transform.Each(c => c.gameObject.layer = 22); // the plushie of lizard has an issue with layers
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
        bool lower = false;

        yield return new WaitForSeconds(1.2f);
        while (IsOwner && itemId.pickedUp)
        {
            #region sound

            int[] apogeeSample = { 00350, 01400, 02050, 02700, 03900, 05150, 06100, 06950, 08100, 09400, 09850, 11800, 14250, 15850, 17800, 19350 };
            float[] narrowness = { .036f, .008f, .020f, .016f, .032f, .010f, .006f, .016f, .006f, .008f, .001f, .010f, .004f, .010f, .012f, .032f };
            float[] levitation = { .050f, .104f, .122f, .148f, .142f, .106f, .108f, .140f, .160f, .156f, .142f, .124f, .176f, .132f, .148f, .086f };

            float baseFreq = lower ? Random.Range(360f, 380f) : Random.Range(390f, 410f);
            float overFreq = Random.Range(.0000001f, .0000003f);

            var clip = AudioClip.Create("hoot", 20000, 1, 44100, false);
            var data = new float[20000];

            for (int i = 0; i < 20000; i++)
            {
                float amp = 0f, dst;

                for (int j = 0; j < apogeeSample.Length; j++)
                {
                    dst = i - apogeeSample[j];
                    amp = Mathf.Max(amp, -.00001f * narrowness[j] * dst * dst + levitation[j]);
                }

                data[i] = Mathf.Sin(i / 20000f * Mathf.PI * (baseFreq - overFreq * (dst = i - 12000) * dst)) * amp;
            }

            clip.SetData(data, 0);
            AudioSource.PlayOneShotHelper(agent.GetOrAddComponent<AudioSource>(), clip, lower ? Random.Range(1.4f, 1.8f) : Random.Range(1.8f, 2.2f));

            #endregion
            #region break

            lower = false;

            if (Random.value < .42f)
            {
                lower = true;
                yield return new WaitForSeconds(Random.Range(.6f, .8f));
            }
            else
                yield return new WaitForSeconds(Random.Range(8f, 16f));

            #endregion
        }
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
        GameAssets.Particle("Enemies/RageEffect.prefab", p =>
        {
            p = Inst(p, owl);

            p.transform.localPosition = Vector3.up  * .3f;
            p.transform.localScale    = Vector3.one * .8f;

            p.GetComponentsInChildren<AudioSource>().Each(s => s.volume = .2f);
            p.GetComponentInChildren<MeshRenderer>().material.color = new(1f, 0f, 120f);
        });
    }

    #endregion
}
