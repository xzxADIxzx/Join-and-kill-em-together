namespace Jaket.Assets;

using HarmonyLib;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

using Object = UnityEngine.Object;

using Jaket.Content;
using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI;
using Jaket.UI.Dialogs;

/// <summary> Class that works with the assets of the mod. </summary>
public class ModAssets
{
    /// <summary> Player doll and its preview prefabs. </summary>
    public static GameObject Doll, Preview;
    /// <summary> Jaket plushies. </summary>
    public static GameObject V2, V3, xzxADIxzx, Sowler;

    /// <summary> Player doll icon. </summary>
    public static Sprite Icon;
    /// <summary> Bestiary description. </summary>
    public static string Desc;
    /// <summary> Shader used by the game for materials. </summary>
    public static Shader Shader;
    /// <summary> Material used by the game for wing trails. </summary>
    public static Material Additv;
    /// <summary> Mixer processing Sam's voice. Used to change volume. </summary>
    public static AudioMixer Mixer;

    /// <summary> Coin texture used by team coins. </summary>
    public static Texture CoinTexture;
    /// <summary> Wing textures used to differentiate teams. </summary>
    public static Texture[] WingTextures;
    /// <summary> Hand textures used by local player. </summary>
    public static Texture[] HandTextures;

    /// <summary> Image of the fallen Virage. </summary>
    public static Sprite ChanFallen;
    /// <summary> Different poses of Virage. </summary>
    public static Sprite[] ChanPoses;
    /// <summary> Icons for the emote selection wheel. </summary>
    public static Sprite[] EmoteIcons, EmoteGlows;

    /// <summary> Text file that contains the description of cosmetic trinkets. </summary>
    public static string ShopEntries;
    /// <summary> Icons for the customization element in the shop. </summary>
    public static Sprite[] ShopIcons;

    /// <summary> Font used by the mod. Differs from the original in support of Cyrillic alphabet. </summary>
    public static Font Font;
    public static TMP_FontAsset FontTMP;

    /// <summary> Loads the assets bundle and other necessary stuff. </summary>
    public static void Load()
    {
        var bundle = AssetBundle.LoadFromFile(Path.Combine(Plugin.Instance.Location, "jaket-assets.bundle"));
        GameAssets.Squeaky(); // preload the sound; otherwise, it crashes .-.

        void Load<T>(string name, Action<T> cons) where T : Object
        {
            var task = bundle.LoadAssetAsync<T>(name);
            task.completed += _ => cons(task.asset as T);
        };

        // general
        Shader = Enemies.Prefabs[EntityType.V2_RedArm - EntityType.EnemyOffset].GetComponent<global::V2>().smr.material.shader;
        Additv = Enemies.Prefabs[EntityType.V2_RedArm - EntityType.EnemyOffset].GetComponentInChildren<TrailRenderer>().material;

        Load<Sprite>("V3-icon", s => Icon = s);
        Load<TextAsset>("V3-bestiary-entry", f => Desc = f.text);
        Load<AudioMixer>("sam-audio", m =>
        {
            Events.Post(() => Networking.LocalPlayer.Voice.outputAudioMixerGroup = (Mixer = m).FindMatchingGroups("Master")[0]);
        });

        // textures
        WingTextures = new Texture[5];
        HandTextures = new Texture[4];

        Load<Texture>("coin", t => CoinTexture = t);

        for (int i = 0; i < 5; i++)
        {
            int j = i;
            Load<Texture>("V3-wings-" + (Team)i, t => WingTextures[j] = t);
        }

        Load<Texture>("V3-hand", t => HandTextures[1] = t);
        Load<Texture>("V3-blast", t => HandTextures[3] = t);
        HandTextures[0] = FistControl.Instance.blueArm.ToAsset().GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture;
        HandTextures[2] = FistControl.Instance.redArm.ToAsset().GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture;

        // sprites
        ChanPoses = new Sprite[7];
        EmoteIcons = new Sprite[12];
        EmoteGlows = new Sprite[12];

        Load<Sprite>("V3-chan-fallen", s => ChanFallen = s);

        for (int i = 0; i < 7; i++)
        {
            int j = i;
            Load<Sprite>("V3-chan-" + i, s => ChanPoses[j] = s);
        }

        for (int i = 0; i < 12; i++)
        {
            var j = i;
            Load<Sprite>("V3-emoji-" + i, s => EmoteIcons[j] = s);
            Load<Sprite>("V3-emoji-" + i + "-glow", s => EmoteGlows[j] = s);
        }

        // shop
        ShopIcons = new Sprite[10];

        for (int i = 0; i < 10; i++)
        {
            int j = i;
            Load<Sprite>("shop-" + i, s => ShopIcons[j] = s);
        }

        Load<TextAsset>("shop-entries", f =>
        {
            Shop.Load(ShopEntries = f.text);
            Shop.LoadPurchases();
        });

        // fonts
        Font = bundle.LoadAsset<Font>("font.ttf");
        FontTMP = TMP_FontAsset.CreateFontAsset(Font);

        // dolls & plushies
        Load<GameObject>("Player Doll.prefab", p =>
        {
            Object.DontDestroyOnLoad(Doll = p);
            FixMaterials(p);
        });

        Load<GameObject>("Player Doll Preview.prefab", p =>
        {
            Object.DontDestroyOnLoad(Preview = p);
            FixMaterials(p);
        });

        Load<Texture>("V2-plushie", t =>
        {
            int i = EntityType.V2 - EntityType.ItemOffset;
            Object.DontDestroyOnLoad(V2 = Items.Prefabs[i] = Object.Instantiate(Items.Prefabs[i]));

            V2.name = "DevPlushie (V2)";
            V2.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V2.GetComponent<Rigidbody>().isKinematic = true;
        });

        Load<Texture>("V3-plushie", t =>
        {
            int i = EntityType.V3 - EntityType.ItemOffset;
            Object.DontDestroyOnLoad(V3 = Items.Prefabs[i] = Object.Instantiate(Items.Prefabs[i]));

            V3.name = "DevPlushie (V3)";
            V3.GetComponentInChildren<Renderer>().material.mainTexture = t;
            V3.GetComponent<Rigidbody>().isKinematic = true;
        });

        Load<GameObject>("DevPlushie (xzxADIxzx).prefab", p =>
        {
            Object.DontDestroyOnLoad(xzxADIxzx = Items.Prefabs[EntityType.xzxADIxzx - EntityType.ItemOffset] = p);
            FixMaterials(p, new(1.3f, 1.3f, 1.3f));

            UIB.Component<ItemIdentifier>(p, itemId =>
            {
                itemId.itemType = ItemType.CustomKey1;
                itemId.pickUpSound = GameAssets.Squeaky();

                itemId.reverseTransformSettings = true;

                itemId.putDownRotation = new(0f, 120f, 90f);
                itemId.putDownScale = new(.5f, .5f, .5f);
            });
        });

        Load<GameObject>("DevPlushie (Sowler).prefab", p =>
        {
            Object.DontDestroyOnLoad(Sowler = Items.Prefabs[EntityType.Sowler - EntityType.ItemOffset] = p);
            FixMaterials(p);

            UIB.Component<ItemIdentifier>(p, itemId =>
            {
                itemId.itemType = ItemType.CustomKey1;
                itemId.pickUpSound = GameAssets.Squeaky();

                itemId.reverseTransformSettings = true;

                itemId.putDownRotation = new(-15f, 120f, 95f);
                itemId.putDownScale = new(.45f, .45f, .45f);
            });
        });
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
        var s = feedbacker ? Settings.FeedColor : Settings.KnuckleColor;
        return HandTextures[(feedbacker ? 0 : 2) + (s == 0 ? (LobbyController.Offline ? 0 : 1) : s == 1 ? 1 : 0)];
    }

    #region fixes

    /// <summary> Changes the colors of materials and their shaders to match the style of the game. </summary>
    public static void FixMaterials(GameObject obj, Color? color = null) => obj.GetComponentsInChildren<Renderer>(true).Do(r =>
    {
        if (r is TrailRenderer) r.material = Additv;
        else r.materials.Do(m =>
        {
            m.color = color ?? Color.white;
            m.shader = Shader;
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
