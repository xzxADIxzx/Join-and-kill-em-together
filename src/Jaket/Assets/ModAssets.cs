namespace Jaket.Assets;

using UnityEngine;
using UnityEngine.Audio;

using TMPF = TMPro.TMP_FontAsset;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Dialogs;
using Jaket.UI.Elements;

using static Jaket.UI.Lib.Pal;

/// <summary> Loader that manages the assets of the project. </summary>
public static class ModAssets
{
    #region content

    /// <summary> Fallen Vermicelli, background. </summary>
    public static Sprite ChanFallen, ChanBackground;
    /// <summary> Different poses of Vermicelli. </summary>
    public static Sprite[] ChanPoses;

    /// <summary> Third-person coin textures. </summary>
    public static Texture[] CoinTextures;
    /// <summary> Third-person wing textures. </summary>
    public static Texture[] WingTextures;
    /// <summary> First-person hand textures. </summary>
    public static Texture[] HandTextures;

    /// <summary> Different sprites. </summary>
    public static Sprite LobbyOwner, LobbyBan, Mask, BestiaryIcon;
    /// <summary> Emote icons/glows. </summary>
    public static Sprite[] EmoteIcons, EmoteGlows;
    /// <summary> Shop & card icons. </summary>
    public static Sprite[] ShopIcons, CardIcons;

    /// <summary> Different prefabs. </summary>
    public static GameObject Doll, Preview, Moon, Template, Can, Chips, Bar, Soda, V2, V3, xzxADIxzx, Sowler;
    /// <summary> Sam's audio mixer. </summary>
    public static AudioMixer Mixer;

    /// <summary> This font differs from the original one in support of Cyrillic alphabet. </summary>
    public static Font DefFont;
    /// <summary> Text mesh pro version of the font. </summary>
    public static TMPF TmpFont;

    /// <summary> Shader used in most of the materials. </summary>
    public static Shader Master;
    /// <summary> Shader used in wing trails materials. </summary>
    public static Shader Additive;

    #endregion
    #region loading

    /// <summary> Asynchronously loads all assets. </summary>
    public static void Load() => AssetBundle.LoadFromFileAsync(Files.Assets).completed += t =>
    {
        var bundle = (t as AssetBundleCreateRequest).assetBundle;

        void Load<T>(string name, Cons<T> cons) where T : Object
        {
            var r = bundle.LoadAssetAsync<T>(name);
            r.completed += _ => cons(r.asset as T);
        }

        void LoadSeq<T>(Func<int, string> name, T[] cons) where T : Object
        {
            for (int i = 0; i < cons.Length; i++)
            {
                int j = i;
                Load<T>(name(j), t => cons[j] = t);
            }
        }

        Load<Sprite>("chan-fallen", s => ChanFallen = s);
        Load<Sprite>("chan-bg", s => ChanBackground = s);
        LoadSeq(i => "chan-" + i, ChanPoses = new Sprite[7]);

        CoinTextures = new Texture[1];
        WingTextures = new Texture[5];
        HandTextures = new Texture[6];

        LoadSeq(i => "doll-wings-" + Teams.All[i], WingTextures);
        Load<Texture>("coin",               t => CoinTextures[0] = t);
        Load<Texture>("arm-main",           t => HandTextures[1] = t);
        Load<Texture>("arm-feedbacker",     t => HandTextures[3] = t);
        Load<Texture>("arm-knuckleblaster", t => HandTextures[5] = t);

        GameAssets.Texture("V1/Arms/T_MainArm.png",    t => HandTextures[0] = t);
        GameAssets.Texture("V1/Arms/T_Feedbacker.png", t => HandTextures[2] = t);
        GameAssets.Texture("V1/v2_armtex.png",         t => HandTextures[4] = t);

        Load<Sprite>("lobby-owner", s => LobbyOwner = s);
        Load<Sprite>("lobby-ban", s => LobbyBan = s);
        Load<Sprite>("mask", s => Mask = s);
        Load<Sprite>("V3-bestiary-icon", s => BestiaryIcon = s);

        LoadSeq(i => "emote-" + i,           EmoteIcons = new Sprite[12]);
        LoadSeq(i => "emote-" + i + "-glow", EmoteGlows = new Sprite[12]);
        LoadSeq(i => "shop-"  + i,           ShopIcons  = new Sprite[12]);
        LoadSeq(i => "card-"  + i,           CardIcons  = new Sprite[09]);

        Load<GameObject>("Doll", p => LoadMaterials(Doll = p));
        Load<GameObject>("Doll Preview", p => LoadMaterials(Preview = p));

        Events.Post(() => Entities.Vendor.Prefabs[(byte)EntityType.Torch], () =>
        {
            Keep(Moon = Entities.Items.Make(EntityType.Torch));
            Dest(Moon.transform.Find("Fire"));
            Dest(Moon.transform.Find("Light"));

            Moon.name = "Moon";
            Moon.Get<ItemIdentifier>(i => i.itemType = ItemType.CustomKey1);
            Moon.Get<Torch>(Dest);
            Moon.transform.position = Vector3.down * 4242f;

            Moon.Get<Entity.Identifier>(i => i.Type = EntityType.Moon);
        });

        GameAssets.Prefab("Fishing/Fish Pickup Template.prefab", p => Template = p);

        GameAssets.Prefab("Levels/Decorations/OfficeVendingMachine.prefab", p =>
        {
            p = p.transform.Find("CoinMechanism/Spawner/VendingMachine_Items").gameObject;

            Keep(Can   = Inst(p));
            Keep(Chips = Inst(p));
            Keep(Bar   = Inst(p));
            Keep(Soda  = Inst(p));

            GameObject[] snacks = [ Can, Chips, Bar, Soda ];

            for (EntityType j = EntityType.SnackCan; j <= EntityType.SnackSoda; j++)
            {
                byte chld = j - EntityType.SnackCan;
                var snack = snacks[chld];

                snack.transform.GetChild(chld).gameObject.SetActive(true);
                snack.name = j.ToString();
                snack.Add<Entity.Identifier>(i => i.Type = j);

                snack.Get<Randomness.RandomSetActive>(Dest);
                snack.Get<AddForce>(Dest);
                snack.Get<Rigidbody>(r => r.isKinematic = true);

                snack.transform.position = Vector3.down * 4242f;
                snack.SetActive(true);
            }
        });

        Load<Texture>("V2-plushie", t => Events.Post(() => Entities.Vendor.Prefabs[(byte)EntityType.V1], () =>
        {
            Keep(V2 = Entities.Items.Make(EntityType.V1));

            V2.name = "DevPlushie (V2)";
            V2.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V2.Get<Rigidbody>(r => r.isKinematic = true);
            V2.transform.position = Vector3.down * 4242f;

            V2.Get<Entity.Identifier>(i => i.Type = EntityType.V2);
        }));
        Load<Texture>("V3-plushie", t => Events.Post(() => Entities.Vendor.Prefabs[(byte)EntityType.V1], () =>
        {
            Keep(V3 = Entities.Items.Make(EntityType.V1));

            V3.name = "DevPlushie (V3)";
            V3.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V3.Get<Rigidbody>(r => r.isKinematic = true);
            V3.transform.position = Vector3.down * 4242f;

            V3.Get<Entity.Identifier>(i => i.Type = EntityType.V3);
        }));

        Load<GameObject>("DevPlushie (xzxADIxzx)", p =>
        {
            LoadMaterials(xzxADIxzx = p, new(1.4f, 1.4f, 1.4f));
            p.Add<ItemIdentifier>(i =>
            {
                GameAssets.Prefab("p/SoundBubbles/SqueakyToy.prefab", p => i.pickUpSound = p);
                i.reverseTransformSettings = true;

                i.putDownRotation = new(  0f, 120f,  90f);
                i.putDownScale    = new(.50f, .50f, .50f);
            });
            p.Add<Entity.Identifier>(i => i.Type = EntityType.xzxADIxzx);
        });
        Load<GameObject>("DevPlushie (Sowler)", p =>
        {
            LoadMaterials(Sowler = p);
            p.Add<ItemIdentifier>(i =>
            {
                GameAssets.Prefab("p/SoundBubbles/SqueakyToy.prefab", p => i.pickUpSound = p);
                i.reverseTransformSettings = true;

                i.putDownRotation = new(-15f, 120f,  95f);
                i.putDownScale    = new(.45f, .45f, .45f);
            });
            p.Add<Entity.Identifier>(i => i.Type = EntityType.Sowler);
        });

        Load<AudioMixer>("sam-audio", m =>
        {
            Networking.LocalPlayer.Voice.outputAudioMixerGroup = (Mixer = m).FindMatchingGroups("master")[0];
            Settings.Load();
        });

        Load<TextAsset>("V3-bestiary-entry", t => BestiaryEntry.Load(t.text, 15));
        Load<TextAsset>("shop-entries", t => Shop.Load(t.text));

        Load<Font>("font.ttf", f =>
        {
            DefFont = f;
            TmpFont = TMPF.CreateFontAsset(f);
        });

        GameAssets.Shader("MasterShader/ULTRAKILL-Standard.shader", s => Master = s);
        GameAssets.Shader("Transparent/ULTRAKILL-simple-additive.shader", s => Additive = s);
    };

    /// <summary> Properly loads materials, their shaders, and colors. </summary>
    private static void LoadMaterials(GameObject obj, Color? color = null) => Events.Post(() => Master && Additive, () =>
    {
        obj.GetComponentsInChildren<Renderer>(true).Each(r => r.materials.Each(m =>
        {
            m.shader = r is TrailRenderer ? Additive : Master;
            m.color = color ?? white;
        }));
    });

    /// <summary> Returns the texture of the hand with the given type. </summary>
    public static Texture HandTexture(int type)
    {
        int color = type < 2 ? Settings.FeedColor : Settings.KnklColor;
        int index = type * 2 + (color == 0 ? (LobbyController.Online ? 1 : 0) : color == 1 ? 1 : 0);

        return HandTextures[index];
    }

    #endregion
}
