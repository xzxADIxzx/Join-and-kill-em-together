namespace Jaket.Assets;

using UnityEngine;
using UnityEngine.Audio;

using FontAsset = TMPro.TMP_FontAsset;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;
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
    /// <summary> Material used in wing trails. </summary>
    public static Material Additv;

    /// <summary> Loads the asset bundle and its content required by the project. </summary>
    public static void Load()
    {
        var bundle = AssetBundle.LoadFromFile(Files.GetFile(Files.Root, "assets.bundle"));

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

        // TODO load vanilla textures from game assets

        Load<GameObject>("Doll", p =>
        {
            Keep(Doll = p);
            FixMaterials(p);
        });
        Load<GameObject>("Doll Preview", p =>
        {
            Keep(DollPreview = p);
            FixMaterials(p);
        });
        Load<Texture>("V2-plushie", t =>
        {
            int i = EntityType.V2 - EntityType.BlueSkull;
            Keep(V2 = Items.Prefabs[i] = Inst(Items.Prefabs[i]));

            V2.name = "DevPlushie (V2)";
            V2.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V2.GetComponent<Rigidbody>().isKinematic = true;
        });
        Load<Texture>("V3-plushie", t =>
        {
            int i = EntityType.V3 - EntityType.BlueSkull;
            Keep(V3 = Items.Prefabs[i] = Inst(Items.Prefabs[i]));

            V3.name = "DevPlushie (V3)";
            V3.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V3.GetComponent<Rigidbody>().isKinematic = true;
        });
        Load<GameObject>("DevPlushie (xzxADIxzx)", p =>
        {
            Keep(xzxADIxzx = Items.Prefabs[EntityType.xzxADIxzx - EntityType.BlueSkull] = p);
            FixMaterials(p, new(1.3f, 1.3f, 1.3f));

            Component<ItemIdentifier>(p, i =>
            {
                i.itemType = ItemType.CustomKey1;
                i.pickUpSound = GameAssets.Squeaky();

                i.reverseTransformSettings = true;

                i.putDownRotation = new(  0f, 120f,  90f);
                i.putDownScale    = new( .5f,  .5f,  .5f);
            });
        });
        Load<GameObject>("DevPlushie (Sowler)", p =>
        {
            Keep(Sowler = Items.Prefabs[EntityType.Sowler - EntityType.BlueSkull] = p);
            FixMaterials(p);

            Component<ItemIdentifier>(p, i =>
            {
                i.itemType = ItemType.CustomKey1;
                i.pickUpSound = GameAssets.Squeaky();

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

        Load<TextAsset>("shop-entries",      t => BestiaryEntry.Load(t.text, 15));
        Load<TextAsset>("V3-bestiary-entry", t =>
        {
            Shop.Load(t.text);
            Shop.LoadPurchases();
        });

        DefFont = bundle.LoadAsset<Font>("font.ttf");
        TmpFont = FontAsset.CreateFontAsset(DefFont);
        /*
        Shader = Enemies.Prefabs[EntityType.V2_RedArm - EntityType.Filth].GetComponent<global::V2>().smr.material.shader;
        Additv = Enemies.Prefabs[EntityType.V2_RedArm - EntityType.Filth].GetComponentInChildren<TrailRenderer>().material;
        */
    }

    /// <summary> Creates a new player doll from the prefab. </summary>
    public static RemotePlayer CreateDoll()
    {
        var obj = Entities.Mark(Doll);
        var enemyId = obj.AddComponent<EnemyIdentifier>();
        var machine = obj.AddComponent<Machine>();

        enemyId.enemyClass = EnemyClass.Machine;
        enemyId.enemyType = EnemyType.V2;
        enemyId.dontCountAsKills = true;

        enemyId.weaknesses = new string[0];
        enemyId.burners = new();
        enemyId.flammables = new();
        enemyId.activateOnDeath = new GameObject[0];
        machine.destroyOnDeath = new GameObject[0];
        machine.hurtSounds = new AudioClip[0];

        // add enemy identifiers to doll parts to make the doll hitable
        foreach (var rb in obj.transform.GetChild(0).GetComponentsInChildren<Rigidbody>())
        {
            rb.gameObject.AddComponent<EnemyIdentifierIdentifier>();
            rb.tag = MapTag(rb.tag);
        }

        return obj.AddComponent<RemotePlayer>();
    }

    /// <summary> Returns the hand texture currently in use. Depends on whether the player is in the lobby or not. </summary>
    public static Texture HandTexture(bool feedbacker = true)
    {
        var s = feedbacker ? Settings.FeedColor : Settings.KnklColor;
        return HandTextures[(feedbacker ? 0 : 2) + (s == 0 ? (LobbyController.Offline ? 0 : 1) : s == 1 ? 1 : 0)];
    }

    #region fixes

    /// <summary> Changes the colors of materials and their shaders to match the style of the game. </summary>
    public static void FixMaterials(GameObject obj, Color? color = null) => obj.GetComponentsInChildren<Renderer>(true).Each(r =>
    {
        if (r is TrailRenderer) r.material = Additv;
        else r.materials.Each(m =>
        {
            m.color = color ?? Color.white;
            m.shader = Master;
        });
    });

    /// <summary> Tags after loading from a bundle changes due to the mismatch in the tags list, this method returns everything to its place. </summary>
    public static string MapTag(string tag) => tag switch
    {
        "RoomManager" => "Body",
        "Body" => "Limb",
        "Forward" => "Head",
        _ => tag
    };

    #endregion
}
