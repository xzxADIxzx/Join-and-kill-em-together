namespace Jaket.Assets;

using UnityEngine;
using UnityEngine.Audio;

using FontAsset = TMPro.TMP_FontAsset;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.UI.Dialogs;
using Jaket.UI.Elements;

/// <summary> Loader that manages the assets of the project. </summary>
public static class ModAssets
{
    /// <summary> Fallen Vermicelli and her background. </summary>
    public static Sprite ChanFallen, ChanBackground;
    /// <summary> Different poses of Vermicelli. </summary>
    public static Sprite[] ChanPoses;

    /// <summary> Coin texture required by the team coins. </summary>
    public static Texture CoinTexture;
    /// <summary> Wing textures used to differentiate teams. </summary>
    public static Texture[] WingTextures;
    /// <summary> Hand textures used to differentiate machines. </summary>
    public static Texture[] HandTextures;

    /// <summary> Prefabs of the player doll and its preview. </summary>
    public static GameObject Doll, DollPreview;
    /// <summary> Additional plushies presented in the project. </summary>
    public static GameObject V2, V3, xzxADIxzx, Sowler;
    /// <summary> Audio mixer processing the voice of Sam. </summary>
    public static AudioMixer Mixer;

    /// <summary> Icons of the doll, crown and ban hammer. </summary>
    public static Sprite BestiaryIcon, LobbyOwner, LobbyBan;
    /// <summary> Icons of the emotes and their glows. </summary>
    public static Sprite[] EmoteIcons, EmoteGlows;
    /// <summary> Icons of the customization elements. </summary>
    public static Sprite[] ShopIcons;

    /// <summary> This font differs from the original one in support of Cyrillic alphabet. </summary>
    public static Font DefFont;
    /// <summary> Text mesh pro version of the font. </summary>
    public static FontAsset TmpFont;

    /// <summary> Shader used in most of the materials. </summary>
    public static Shader Master;
    /// <summary> Shader used in wing trails materials. </summary>
    public static Shader Additv;

    /// <summary> Loads the asset bundle and its content required by the project. </summary>
    public static void Load()
    {
        var bundle = AssetBundle.LoadFromFile(Files.Join(Files.Root, "assets.bundle"));

        void Load<T>(string name, Cons<T> cons) where T : Object
        {
            var r = bundle.LoadAssetAsync<T>(name);
            r.completed += _ => cons(r.asset as T);
        }

        void LoadAll<T>(Func<int, string> name, T[] array) where T : Object
        {
            for (int i = 0; i < array.Length; i++)
            {
                int j = i;
                Load<T>(name(j), r => array[j] = r);
            }
        }

        Load<Sprite>("chan-fallen", s => ChanFallen     = s);
        Load<Sprite>("chan-bg",     s => ChanBackground = s);
        LoadAll(i => "chan-" + i, ChanPoses = new Sprite[7]);

        LoadAll(i => "doll-wings-" + Teams.All[i], WingTextures = new Texture[5]);
        HandTextures = new Texture[6];

        Load<Texture>("coin",               t => CoinTexture     = t);
        Load<Texture>("arm-main",           t => HandTextures[1] = t);
        Load<Texture>("arm-feedbacker",     t => HandTextures[3] = t);
        Load<Texture>("arm-knuckleblaster", t => HandTextures[5] = t);

        GameAssets.Texture("V1/Arms/T_MainArm.png",    t => HandTextures[0] = t);
        GameAssets.Texture("V1/Arms/T_Feedbacker.png", t => HandTextures[2] = t);
        GameAssets.Texture("V1/v2_armtex.png",         t => HandTextures[4] = t);

        Load<GameObject>("Doll",         p => UpdtMaterials(Doll        = p));
        Load<GameObject>("Doll Preview", p => UpdtMaterials(DollPreview = p));
        Load<Texture>("V2-plushie",      t => UpdtMaterials(t, true));
        Load<Texture>("V3-plushie",      t => UpdtMaterials(t, false));
        Load<GameObject>("DevPlushie (xzxADIxzx)", p =>
        {
            UpdtMaterials(xzxADIxzx = p, new(1.4f, 1.4f, 1.4f));
            Component<ItemIdentifier>(p, i =>
            {
                GameAssets.Particle("SoundBubbles/SqueakyToy.prefab", p => i.pickUpSound = p);
                i.reverseTransformSettings = true;

                i.putDownRotation = new(  0f, 120f,  90f);
                i.putDownScale    = new( .5f,  .5f,  .5f);
            });
        });
        Load<GameObject>("DevPlushie (Sowler)", p =>
        {
            UpdtMaterials(Sowler = p);
            Component<ItemIdentifier>(p, i =>
            {
                GameAssets.Particle("SoundBubbles/SqueakyToy.prefab", p => i.pickUpSound = p);
                i.reverseTransformSettings = true;

                i.putDownRotation = new(-15f, 120f,  95f);
                i.putDownScale    = new(.45f, .45f, .45f);
            });
        });
        Load<AudioMixer>("sam-audio", m => Events.Post(() =>
        {
            Networking.LocalPlayer.Voice.outputAudioMixerGroup = (Mixer = m).FindMatchingGroups("master")[0];
            Settings.Load();
        }));

        Load<Sprite>("V3-bestiary-icon",     s => BestiaryIcon = s);
        Load<Sprite>("lobby-owner",          s => LobbyOwner   = s);
        Load<Sprite>("lobby-ban",            s => LobbyBan     = s);
        LoadAll(i => "emote-" + i,           EmoteIcons = new Sprite[12]);
        LoadAll(i => "emote-" + i + "-glow", EmoteGlows = new Sprite[12]);
        LoadAll(i => "shop-"  + i,           ShopIcons  = new Sprite[12]);

        Load<TextAsset>("V3-bestiary-entry", t => BestiaryEntry.Load(t.text, 15));
        Load<TextAsset>("shop-entries",      t =>
        {
            Shop.Load(t.text);
            Shop.LoadPurchases();
        });

        DefFont = bundle.LoadAsset<Font>("font.ttf");
        TmpFont = FontAsset.CreateFontAsset(DefFont);

        GameAssets.Shader("MasterShader/ULTRAKILL-Standard.shader",       s => Master = s);
        GameAssets.Shader("Transparent/ULTRAKILL-simple-additive.shader", s => Additv = s);
    }

    /// <summary> Creates a new player doll from the prefab. </summary>
    public static GameObject CreateDoll(Vector3 position)
    {
        var obj = Inst(Doll, position);
        var enemyId = obj.AddComponent<EnemyIdentifier>();
        var machine = obj.AddComponent<Machine>();

        enemyId.enemyClass = EnemyClass.Machine;
        enemyId.enemyType  = EnemyType.V2;
        enemyId.dontUnlockBestiary = true;
        enemyId.dontCountAsKills   = true;

        enemyId.weaknesses      = [];
        enemyId.burners         = [];
        enemyId.flammables      = [];
        enemyId.activateOnDeath = [];
        machine.destroyOnDeath  = [];
        machine.hurtSounds      = [];

        // make body parts of the doll hittable and resolve the mismatch of their tags
        foreach (var rb in obj.transform.GetChild(0).GetComponentsInChildren<Rigidbody>())
        {
            rb.gameObject.AddComponent<EnemyIdentifierIdentifier>();
            rb.tag = rb.tag switch
            {
                "RoomManager" => "Body",
                "Body"        => "Limb",
                "Forward"     => "Head",
                _             => rb.tag
            };
        }
        return obj;
    }

    #region replacement

    /// <summary> Returns the texture of the hand with the given type. </summary>
    public static Texture HandTexture(int type)
    {
        int color = type < 2 ? Settings.FeedColor : Settings.KnklColor;
        int index = type * 2 + (color == 0 ? (LobbyController.Online ? 1 : 0) : color == 1 ? 1 : 0);

        return HandTextures[index];
    }

    /// <summary> Changes the colors of materials and their shaders to match the style of the game. </summary>
    public static void UpdtMaterials(GameObject prefab, Color? color = null) => Events.Post(() => Master != null && Additv != null, () =>
    {
        prefab.GetComponentsInChildren<Renderer>(true).Each(r =>
        {
            if (r is TrailRenderer) r.material.shader = Additv;
            else r.materials.Each(m =>
            {
                m.color = color ?? Color.white;
                m.shader = Master;
            });
        });
    });

    /// <summary> Changes the textures of materials and saves the prefab for the given entity type. </summary>
    public static void UpdtMaterials(Texture texture, bool V2orV3) => Events.Post(() => Entities.Vendor.Prefabs[(byte)EntityType.V1] != null, () =>
    {
        ref GameObject prefab = ref (V2orV3 ? ref V2 : ref V3);

        Keep(prefab = Entities.Items.Make(EntityType.V1));

        prefab.name = $"DevPlushie ({(V2orV3 ? "V2" : "V3")})";
        prefab.GetComponentInChildren<Renderer>().material.mainTexture = texture;
        prefab.GetComponent<Rigidbody>().isKinematic = true;
    });

    #endregion
}
